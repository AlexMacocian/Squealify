using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Squealify.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Squealify;

/// <summary>
/// Builds a <see cref="QueryMethodModel"/> from a partial method marked with [SqlQuery].
/// </summary>
internal static class QueryModelBuilder
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static QueryMethodModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var method = (IMethodSymbol)context.TargetSymbol;
        var type = method.ContainingType;
        var location = method.Locations.FirstOrDefault();
        var diagnostics = new List<DiagnosticModel>();
        var attribute = context.Attributes[0];
        var sql = attribute.ConstructorArguments.FirstOrDefault().Value as string ?? string.Empty;
        string? postgreSql = null;
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == Constants.SqlQueryPostgreSqlProperty && argument.Value.Value is string value)
            {
                postgreSql = value;
            }
        }

        if (method.IsStatic || method.IsGenericMethod || !method.IsPartialDefinition || method.PartialImplementationPart is not null)
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.InvalidQueryMethod, location, method.Name));
        }

        if (type.ContainingType is not null || type.IsGenericType || type.ContainingNamespace.IsGlobalNamespace || type.TypeKind != TypeKind.Class)
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.InvalidQueryContainer, location, method.Name));
        }
        else if (!DerivesFromQueryContext(type))
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.MissingQueryContext, location, type.Name));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var parameters = new List<QueryParameterModel>();
        foreach (var parameter in method.Parameters)
        {
            var parameterType = parameter.Type.ToDisplayString(TypeFormat);
            if (parameter.RefKind == RefKind.None && parameter.Type.ToDisplayString() == "System.Threading.CancellationToken")
            {
                parameters.Add(new QueryParameterModel(Escape(parameter.Name), parameterType, IsCancellationToken: true, Value: null));
                continue;
            }

            var value = ModelBuilder.DescribeValue(parameter.Name, parameter.Name, parameter.Type, parameter.NullableAnnotation);
            if (parameter.RefKind != RefKind.None || !IsSupported(value))
            {
                diagnostics.Add(Diagnostics.Create(Diagnostics.UnsupportedQueryParameter, parameter.Locations.FirstOrDefault() ?? location, parameter.Name, method.Name));
            }

            parameters.Add(new QueryParameterModel(Escape(parameter.Name), parameterType, IsCancellationToken: false, Value: value));
        }

        ValidateParameters(sql, method, parameters, location, diagnostics);
        if (postgreSql is not null)
        {
            ValidateParameters(postgreSql, method, parameters, location, diagnostics);
        }

        var (isAsync, shape, nullableResult, row) = AnalyzeReturn(method, SqlText.IsQuery(sql), location, diagnostics);
        return new QueryMethodModel(
            Namespace: type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString(),
            ClassName: type.Name,
            Name: method.Name,
            Accessibility: AccessibilityText(context.TargetNode),
            ReturnType: method.ReturnType.ToDisplayString(TypeFormat),
            IsAsync: isAsync,
            Shape: shape,
            NullableResult: nullableResult,
            Row: row,
            Parameters: parameters.ToEquatableArray(),
            Sql: sql,
            PostgreSql: postgreSql,
            Diagnostics: diagnostics.ToEquatableArray());
    }

    private static (bool IsAsync, QueryShape Shape, bool NullableResult, RowModel? Row) AnalyzeReturn(
        IMethodSymbol method,
        bool isQuery,
        Location? location,
        List<DiagnosticModel> diagnostics)
    {
        var returnType = method.ReturnType;
        var isAsync = false;
        ITypeSymbol? inner = returnType.SpecialType == SpecialType.System_Void ? null : returnType;
        if (returnType is INamedTypeSymbol { Name: "ValueTask", Arity: <= 1 } task &&
            task.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks")
        {
            isAsync = true;
            inner = task.Arity == 1 ? task.TypeArguments[0] : null;
        }

        if (inner is null)
        {
            return (isAsync, QueryShape.Execute, false, null);
        }

        if (!isQuery)
        {
            if (inner.SpecialType != SpecialType.System_Int32)
            {
                diagnostics.Add(Diagnostics.Create(Diagnostics.UnsupportedQueryReturn, location, method.Name, "a command (not a query) can only return void or int"));
            }

            return (isAsync, QueryShape.Affected, false, null);
        }

        if (inner is INamedTypeSymbol { Arity: 1, Name: "List" } list &&
            list.ContainingNamespace.ToDisplayString() == "System.Collections.Generic")
        {
            var element = list.TypeArguments[0];
            return (isAsync, QueryShape.List, false, BuildRow(element, element.NullableAnnotation, method, location, diagnostics));
        }

        var nullable = inner.NullableAnnotation == NullableAnnotation.Annotated;
        var rowType = inner;
        if (inner is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType)
        {
            rowType = nullableType.TypeArguments[0];
            nullable = true;
        }

        return (isAsync, QueryShape.Single, nullable, BuildRow(rowType, inner.NullableAnnotation, method, location, diagnostics));
    }

    private static RowModel? BuildRow(ITypeSymbol type, NullableAnnotation annotation, IMethodSymbol method, Location? location, List<DiagnosticModel> diagnostics)
    {
        var declared = type.WithNullableAnnotation(annotation).ToDisplayString(TypeFormat);
        var scalar = ModelBuilder.DescribeValue("value", "value", type, annotation);
        if (IsSupported(scalar))
        {
            return new RowModel(RowKind.Scalar, scalar.ValueType, declared, new[] { scalar }.ToEquatableArray());
        }

        var valueType = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        if (valueType is INamedTypeSymbol { IsTupleType: true } tuple)
        {
            var elements = new List<ColumnModel>();
            foreach (var element in tuple.TupleElements)
            {
                var value = ModelBuilder.DescribeValue(element.Name, element.Name, element.Type, element.NullableAnnotation);
                if (!IsSupported(value))
                {
                    diagnostics.Add(Diagnostics.Create(Diagnostics.UnsupportedQueryReturn, location, method.Name, $"tuple element '{element.Name}' has an unsupported type"));
                    return null;
                }

                elements.Add(value);
            }

            return new RowModel(RowKind.Tuple, valueType.ToDisplayString(TypeFormat), declared, elements.ToEquatableArray());
        }

        if (valueType is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } named ||
            (named.TypeKind == TypeKind.Class && !named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility != Accessibility.Private)))
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.UnsupportedQueryReturn, location, method.Name, $"'{valueType.ToDisplayString()}' is neither a scalar, a tuple nor a class or struct with a parameterless constructor"));
            return null;
        }

        var members = new List<ColumnModel>();
        var names = new HashSet<string>();
        for (INamedTypeSymbol? current = named; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (ModelBuilder.GetColumnMember(member) is not (ITypeSymbol memberType, NullableAnnotation memberAnnotation) || !names.Add(member.Name))
                {
                    continue;
                }

                var columnName = ModelBuilder.GetExplicitColumnName(member) ?? member.Name;
                var value = ModelBuilder.DescribeValue(member.Name, NormalizeName(columnName), memberType, memberAnnotation);
                if (!IsSupported(value))
                {
                    diagnostics.Add(Diagnostics.Create(Diagnostics.UnsupportedQueryReturn, location, method.Name, $"member '{member.Name}' has an unsupported type"));
                    return null;
                }

                members.Add(value);
            }
        }

        if (members.Count == 0)
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.UnsupportedQueryReturn, location, method.Name, $"'{valueType.ToDisplayString()}' has no public settable members"));
            return null;
        }

        return new RowModel(RowKind.Object, valueType.ToDisplayString(TypeFormat), declared, members.ToEquatableArray());
    }

    private static void ValidateParameters(string sql, IMethodSymbol method, List<QueryParameterModel> parameters, Location? location, List<DiagnosticModel> diagnostics)
    {
        var referenced = new HashSet<string>(SqlText.Parameters(sql));
        var declared = new HashSet<string>(method.Parameters.Where(p => !IsCancellationToken(p)).Select(p => p.Name));
        foreach (var name in referenced.Where(n => !declared.Contains(n)).OrderBy(n => n))
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.UnknownSqlParameter, location, method.Name, name));
        }

        foreach (var name in declared.Where(n => !referenced.Contains(n)).OrderBy(n => n))
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.UnusedQueryParameter, location, name, method.Name));
        }
    }

    private static bool IsCancellationToken(IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == "System.Threading.CancellationToken";

    /// <summary>
    /// Same normalization as SqlQueryContext.NormalizeName at runtime.
    /// </summary>
    public static string NormalizeName(string name) => new(name.Where(c => c != '_').Select(char.ToLowerInvariant).ToArray());

    private static bool IsSupported(ColumnModel value) => value.Kind != ColumnKind.Custom;

    private static bool DerivesFromQueryContext(INamedTypeSymbol type)
    {
        // Bases generated in the same compilation (table contexts) are not visible to generators; with several partial
        // declarations Roslyn may report the unresolved base as an interface.
        if (type.Interfaces.Any(i => i.TypeKind == TypeKind.Error))
        {
            return true;
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            // Bases generated in the same compilation (table contexts) are not visible to generators.
            if (current.TypeKind == TypeKind.Error)
            {
                return true;
            }

            if (current.Name == Constants.SqlQueryContextName && current.ContainingNamespace.ToDisplayString() == Constants.Namespace)
            {
                return true;
            }
        }

        return false;
    }

    private static string Escape(string name)
        => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    /// <summary>
    /// Accessibility modifiers as written on the declaration; the implementation must repeat them exactly.
    /// </summary>
    private static string AccessibilityText(SyntaxNode node)
    {
        if (node is not Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax declaration)
        {
            return string.Empty;
        }

        return string.Join(" ", declaration.Modifiers
            .Where(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword) || m.IsKind(SyntaxKind.PrivateKeyword))
            .Select(m => m.Text));
    }
}
