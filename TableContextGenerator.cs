using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Squealify.Contexts;
using Squealify.Table;
using Sybil;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class TableContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (s, _) => s is ClassDeclarationSyntax,
            transform: static (ctx, _) => GetFilteredClassDeclarationSyntax(ctx)).Where(static c => c is not null);
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
        context.RegisterSourceOutput(compilationAndClasses, (sourceProductionContext, tuple) => Execute(tuple.Left, tuple.Right, sourceProductionContext));
    }

    private static ClassDeclarationSyntax? GetFilteredClassDeclarationSyntax(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        if (classDeclarationSyntax.AttributeLists
            .SelectMany(l => l.Attributes)
            .OfType<AttributeSyntax>()
            .Any(s => s.Name.ToString() is Constants.TableAttributeName or Constants.TableAttributeShortName))
        {
            return classDeclarationSyntax;
        }

        return default;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classDeclarationSyntaxes, SourceProductionContext sourceProductionContext)
    {
        if (classDeclarationSyntaxes.IsDefaultOrEmpty)
        {
            return;
        }

        var maybeLanguageVersion = (compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions)?.LanguageVersion;
        if (!maybeLanguageVersion.HasValue)
        {
            return;
        }

        var languageVersion = maybeLanguageVersion.Value;
        foreach(var classDeclarationSyntax in classDeclarationSyntaxes)
        {
            if (classDeclarationSyntax is null)
            {
                continue;
            }

            ExecuteClass(classDeclarationSyntax, compilation, sourceProductionContext, languageVersion);
        }
    }

    private static void ExecuteClass(ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation, SourceProductionContext sourceProductionContext, LanguageVersion languageVersion)
    {
        var contextClassName = $"{classDeclarationSyntax.Identifier.ValueText}TableContextBase";
        var usingsSet = new HashSet<string>
        {
            Constants.UsingSystemDataCommon
        };

        // Add the namespace of the class declaration (if any)
        var namespaceNode = classDeclarationSyntax.Ancestors().FirstOrDefault(n => n is NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax);
        if (namespaceNode is BaseNamespaceDeclarationSyntax baseNamespace)
        {
            var namespaceName = baseNamespace.Name.ToString();
            if (!string.IsNullOrEmpty(namespaceName))
            {
                usingsSet.Add(namespaceName);
            }
        }

        // Collect all "top-level" using directives from the same file
        var root = classDeclarationSyntax.SyntaxTree.GetRoot() as CompilationUnitSyntax;
        if (root is not null)
        {
            foreach (var usingDirective in root.Usings)
            {
                var nameText = usingDirective.Name?.ToString();
                if (nameText is not null)
                {
                    usingsSet.Add(nameText);
                }
            }
        }

        var builder = SyntaxBuilder.CreateCompilationUnit();
        foreach(var u in usingsSet)
        {
            builder.WithUsing(u);
        }

        var namespaceBuilder = languageVersion >= LanguageVersion.CSharp10 ? SyntaxBuilder.CreateFileScopedNamespace(Constants.Namespace) : SyntaxBuilder.CreateNamespace(Constants.Namespace);
        builder.WithNamespace(namespaceBuilder);
        var classBuilder = SyntaxBuilder.CreateClass(contextClassName)
            .WithModifiers($"{Constants.Public} {Constants.Abstract}")
            .WithProperty(SyntaxBuilder.CreateProperty(Constants.DbConnectionType, Constants.DbConnectionPropertyName)
                .WithModifier(Constants.Protected)
                .WithAccessor(SyntaxBuilder.CreateGetter()))
            .WithConstructor(SyntaxBuilder.CreateConstructor(contextClassName)
                .WithParameter(Constants.DbConnectionType, Constants.DbConnectionArgumentName)
                .WithModifier(Constants.Public)
                .WithBody($"this.{Constants.DbConnectionPropertyName} = {Constants.DbConnectionArgumentName};"));
        namespaceBuilder.WithClass(classBuilder);

        var ctx = GetTableContext(classDeclarationSyntax, compilation);

        // Generate create methods
        (var createMethod, var createIfNotExistsMethod) = TableCreateMethodGenerator.GenerateCreateMethods(ctx);
        var insertMethod = BasicQueryMethodGenerator.CreateInsertStatement(ctx);
        var upsertMethod = BasicQueryMethodGenerator.CreateUpsertStatement(ctx);
        var updateMethod = BasicQueryMethodGenerator.CreateUpdateStatement(ctx);
        var deleteMethod = BasicQueryMethodGenerator.CreateDeleteStatement(ctx);
        var findMethod = BasicQueryMethodGenerator.CreateFindStatement(ctx);
        classBuilder
            .WithMethod(createMethod.MethodBuilder)
            .WithMethod(createIfNotExistsMethod.MethodBuilder)
            .WithMethod(insertMethod.MethodBuilder)
            .WithMethod(upsertMethod.MethodBuilder)
            .WithMethod(updateMethod.MethodBuilder)
            .WithMethod(deleteMethod.MethodBuilder)
            .WithMethod(findMethod.MethodBuilder)
            .WithMethod(CreateParameterMethodGenerator.CreateParameterMethod());

        // Generate conversion properties
        var conversionProperties = ctx.Fields
            .Where(f => f.RequiresConversion)
            .GroupBy(f => f.PropertyType)
            .Select(g => g.First())
            .Select(ConversionPropertyGenerator.GenerateConversionProperties)
            .OfType<(PropertyBuilder ConvertTo, PropertyBuilder ConvertFrom)>();

        foreach((var convertTo, var convertFrom) in conversionProperties)
        {
            classBuilder.WithProperty(convertTo);
            classBuilder.WithProperty(convertFrom);
        }

        // Replace placeholders with formatted command text
        var syntax = builder.Build();
        var source = syntax.ToFullString()
            .Replace(createMethod.Placeholder, createMethod.CommandText)
            .Replace(createIfNotExistsMethod.Placeholder, createIfNotExistsMethod.CommandText)
            .Replace(insertMethod.Placeholder, insertMethod.CommandText)
            .Replace(upsertMethod.Placeholder, upsertMethod.CommandText)
            .Replace(updateMethod.Placeholder, updateMethod.CommandText)
            .Replace(deleteMethod.Placeholder, deleteMethod.CommandText)
            .Replace(findMethod.Placeholder, findMethod.CommandText);

        sourceProductionContext.AddSource($"{contextClassName}.g.cs", source);
    }

    private static TableContext GetTableContext(ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation)
    {
        var fields = SyntaxParsers.GetFieldsFromClass(classDeclarationSyntax, compilation);
        var primaryKeyField = fields.First(f => f.IsPrimaryKey);
        var tableName = SyntaxParsers.GetTableName(classDeclarationSyntax);
        var tableUnqiues = SyntaxParsers.GetTableUniques(classDeclarationSyntax);
        return new TableContext(tableName, classDeclarationSyntax.Identifier.ToString(), primaryKeyField, fields, tableUnqiues);
    }
}
