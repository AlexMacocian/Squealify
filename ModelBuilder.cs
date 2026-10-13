using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Squealify.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Squealify;

/// <summary>
/// Builds a <see cref="TableModel"/> from a class marked with [Table], using the semantic model.
/// </summary>
internal static class ModelBuilder
{
    private const string AttributePrefix = Constants.Namespace + ".";

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static TableModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var diagnostics = new List<DiagnosticModel>();
        var location = type.Locations.FirstOrDefault();

        var tableAttribute = context.Attributes[0];
        var tableName = tableAttribute.ConstructorArguments.Length > 0 &&
            tableAttribute.ConstructorArguments[0].Value is string { Length: > 0 } explicitName
            ? explicitName
            : type.Name;

        var columns = new List<ColumnModel>();
        foreach (var member in type.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is not IPropertySymbol property ||
                property.IsStatic ||
                property.IsIndexer ||
                property.DeclaredAccessibility != Accessibility.Public ||
                property.GetMethod is null ||
                property.SetMethod is null)
            {
                continue;
            }

            columns.Add(BuildColumn(property));
        }

        var primaryKeys = columns.Where(c => c.IsPrimaryKey).ToList();
        if (primaryKeys.Count == 0)
        {
            diagnostics.Add(Diagnostics.Create(Diagnostics.MissingPrimaryKey, location, type.Name));
        }

        foreach (var column in columns.Where(c => c.IsAutoIncrement))
        {
            if (primaryKeys.Count != 1 ||
                !column.IsPrimaryKey ||
                column.EnumType is not null ||
                column.IsNullable ||
                column.Kind is not (ColumnKind.Int32 or ColumnKind.Int64))
            {
                diagnostics.Add(Diagnostics.Create(Diagnostics.InvalidAutoIncrement, location, column.PropertyName));
            }
        }

        var keySets = new List<KeySetModel>();
        var foreignKeys = new List<ForeignKeyModel>();
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].IsUnique)
            {
                keySets.Add(new KeySetModel(ImmutableArray.Create(i).ToEquatableArray(), IsUnique: true, CreateIndex: false));
            }

            if (columns[i].ReferenceTable is string referenceTable &&
                columns[i].ReferenceColumn is string referenceColumn)
            {
                foreignKeys.Add(new ForeignKeyModel(
                    ImmutableArray.Create(i).ToEquatableArray(),
                    referenceTable,
                    ImmutableArray.Create(referenceColumn).ToEquatableArray(),
                    columns[i].OnDelete));
            }
        }

        foreach (var attribute in type.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            switch (attributeName)
            {
                case AttributePrefix + Constants.TableUniqueAttributeName:
                    AddKeySet(attribute, Constants.TableUniqueAttributeName, isUnique: true, createIndex: false);
                    break;
                case AttributePrefix + Constants.TableIndexAttributeName:
                    AddKeySet(attribute, Constants.TableIndexAttributeName, isUnique: false, createIndex: true);
                    break;
                case AttributePrefix + Constants.TableQueryAttributeName:
                    AddKeySet(attribute, Constants.TableQueryAttributeName, isUnique: false, createIndex: false);
                    break;
                case AttributePrefix + Constants.TableForeignKeyAttributeName:
                    AddTableForeignKey(attribute);
                    break;
            }
        }

        var primaryKeyIndexes = columns
            .Select((c, i) => (c, i))
            .Where(t => t.c.IsPrimaryKey)
            .Select(t => t.i)
            .ToEquatableArray();

        return new TableModel(
            HintName: GetHintName(type),
            ContextName: $"{type.Name}TableContextBase",
            DboType: type.ToDisplayString(TypeFormat),
            TableName: tableName,
            Columns: columns.ToEquatableArray(),
            KeySets: DeduplicateKeySets(keySets, primaryKeyIndexes).ToEquatableArray(),
            ForeignKeys: foreignKeys.ToEquatableArray(),
            Diagnostics: diagnostics.ToEquatableArray());

        void AddKeySet(AttributeData attribute, string attributeName, bool isUnique, bool createIndex)
        {
            var names = GetStringArray(attribute.ConstructorArguments.FirstOrDefault());
            var indexes = ResolveColumns(names, attributeName, attribute);
            if (indexes is { Length: > 0 } resolved)
            {
                keySets.Add(new KeySetModel(resolved.ToEquatableArray(), isUnique, createIndex));
            }
        }

        void AddTableForeignKey(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length < 3 ||
                attribute.ConstructorArguments[0].Value is not string referenceTable)
            {
                return;
            }

            var columnNames = GetStringArray(attribute.ConstructorArguments[1]);
            var referenceColumns = GetStringArray(attribute.ConstructorArguments[2]);
            if (columnNames.Length != referenceColumns.Length || columnNames.Length == 0)
            {
                diagnostics.Add(Diagnostics.Create(Diagnostics.ForeignKeyColumnMismatch, GetLocation(attribute), type.Name));
                return;
            }

            var indexes = ResolveColumns(columnNames, Constants.TableForeignKeyAttributeName, attribute);
            if (indexes is null)
            {
                return;
            }

            foreignKeys.Add(new ForeignKeyModel(
                indexes.Value.ToEquatableArray(),
                referenceTable,
                referenceColumns.ToEquatableArray(),
                GetOnDelete(attribute)));
        }

        ImmutableArray<int>? ResolveColumns(ImmutableArray<string> names, string attributeName, AttributeData attribute)
        {
            var builder = ImmutableArray.CreateBuilder<int>(names.Length);
            foreach (var name in names)
            {
                var index = columns.FindIndex(c => c.PropertyName == name || c.ColumnName == name);
                if (index < 0)
                {
                    diagnostics.Add(Diagnostics.Create(
                        Diagnostics.UnknownColumn,
                        GetLocation(attribute),
                        name,
                        attributeName.Substring(0, attributeName.Length - "Attribute".Length),
                        type.Name));
                    return null;
                }

                builder.Add(index);
            }

            return builder.ToImmutable();
        }
    }

    private static ColumnModel BuildColumn(IPropertySymbol property)
    {
        var columnName = property.Name;
        var isPrimaryKey = false;
        var isAutoIncrement = false;
        var isUnique = false;
        int? varcharLength = null;
        string? referenceTable = null;
        string? referenceColumn = null;
        var onDelete = 0;
        foreach (var attribute in property.GetAttributes())
        {
            switch (attribute.AttributeClass?.ToDisplayString())
            {
                case AttributePrefix + Constants.PrimaryKeyAttributeName:
                    isPrimaryKey = true;
                    break;
                case AttributePrefix + Constants.AutoIncrementAttributeName:
                    isAutoIncrement = true;
                    break;
                case AttributePrefix + Constants.ColumnUniqueAttributeName:
                    isUnique = true;
                    break;
                case AttributePrefix + Constants.ColumnNameAttributeName:
                    if (attribute.ConstructorArguments.FirstOrDefault().Value is string { Length: > 0 } name)
                    {
                        columnName = name;
                    }

                    break;
                case AttributePrefix + Constants.VarcharAttributeName:
                    if (attribute.ConstructorArguments.FirstOrDefault().Value is byte length)
                    {
                        varcharLength = length;
                    }

                    break;
                case AttributePrefix + Constants.ForeignKeyAttributeName:
                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        referenceTable = attribute.ConstructorArguments[0].Value as string;
                        referenceColumn = attribute.ConstructorArguments[1].Value as string;
                        onDelete = GetOnDelete(attribute);
                    }

                    break;
            }
        }

        var type = property.Type;
        var isNullable = false;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType)
        {
            type = nullableType.TypeArguments[0];
            isNullable = true;
        }
        else if (type.IsReferenceType && property.NullableAnnotation == NullableAnnotation.Annotated)
        {
            isNullable = true;
        }

        var valueType = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        string? enumType = null;
        ColumnKind kind;
        if (valueType is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } underlyingType })
        {
            enumType = valueType.ToDisplayString(TypeFormat);
            kind = GetKind(underlyingType);
        }
        else
        {
            kind = GetKind(valueType);
        }

        string? convertToName = null;
        string? convertFromName = null;
        if (kind == ColumnKind.Custom)
        {
            var safeName = GetSafeTypeName(valueType);
            convertToName = $"{safeName}ToStringConverter";
            convertFromName = $"StringTo{safeName}Converter";
        }

        return new ColumnModel(
            ColumnName: columnName,
            PropertyName: property.Name,
            ValueType: valueType.ToDisplayString(TypeFormat),
            Kind: kind,
            EnumType: enumType,
            IsNullable: isNullable,
            IsValueType: valueType.IsValueType,
            IsPrimaryKey: isPrimaryKey,
            IsAutoIncrement: isAutoIncrement,
            IsUnique: isUnique,
            VarcharLength: varcharLength,
            ReferenceTable: referenceTable,
            ReferenceColumn: referenceColumn,
            OnDelete: onDelete,
            ConvertToName: convertToName,
            ConvertFromName: convertFromName);
    }

    private static ColumnKind GetKind(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean: return ColumnKind.Boolean;
            case SpecialType.System_Byte: return ColumnKind.Byte;
            case SpecialType.System_SByte: return ColumnKind.SByte;
            case SpecialType.System_Int16: return ColumnKind.Int16;
            case SpecialType.System_UInt16: return ColumnKind.UInt16;
            case SpecialType.System_Int32: return ColumnKind.Int32;
            case SpecialType.System_UInt32: return ColumnKind.UInt32;
            case SpecialType.System_Int64: return ColumnKind.Int64;
            case SpecialType.System_UInt64: return ColumnKind.UInt64;
            case SpecialType.System_Single: return ColumnKind.Single;
            case SpecialType.System_Double: return ColumnKind.Double;
            case SpecialType.System_Decimal: return ColumnKind.Decimal;
            case SpecialType.System_Char: return ColumnKind.Char;
            case SpecialType.System_String: return ColumnKind.String;
            case SpecialType.System_DateTime: return ColumnKind.DateTime;
        }

        if (type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte })
        {
            return ColumnKind.Bytes;
        }

        return type.ToDisplayString() switch
        {
            "System.Guid" => ColumnKind.Guid,
            "System.DateTimeOffset" => ColumnKind.DateTimeOffset,
            "System.DateOnly" => ColumnKind.DateOnly,
            "System.TimeOnly" => ColumnKind.TimeOnly,
            "System.TimeSpan" => ColumnKind.TimeSpan,
            _ => ColumnKind.Custom
        };
    }

    /// <summary>
    /// Converter names keep the pre-0.9 scheme: simple type name, "[]" replaced with "Array".
    /// </summary>
    private static string GetSafeTypeName(ITypeSymbol type)
    {
        var display = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Replace("[]", Constants.ArrayName);
        var builder = new StringBuilder(display.Length);
        foreach (var c in display)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                builder.Append(c);
            }
        }

        if (builder.Length > 0)
        {
            builder[0] = char.ToUpperInvariant(builder[0]);
        }

        return builder.ToString();
    }

    private static int GetOnDelete(AttributeData attribute)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == Constants.ForeignKeyOnDeleteProperty && argument.Value.Value is int value)
            {
                return value;
            }
        }

        return 0;
    }

    private static ImmutableArray<string> GetStringArray(TypedConstant constant)
    {
        if (constant.Kind != TypedConstantKind.Array || constant.Values.IsDefault)
        {
            return ImmutableArray<string>.Empty;
        }

        return constant.Values
            .Select(v => v.Value as string)
            .Where(v => v is not null)
            .Select(v => v!)
            .ToImmutableArray();
    }

    private static Location? GetLocation(AttributeData attribute)
        => attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();

    /// <summary>
    /// Keeps one key set per column combination (unique wins over index, index over query) and drops key sets equal to the primary key.
    /// </summary>
    private static IEnumerable<KeySetModel> DeduplicateKeySets(List<KeySetModel> keySets, EquatableArray<int> primaryKey)
    {
        return keySets
            .Where(k => !k.Columns.Equals(primaryKey))
            .GroupBy(k => k.Columns)
            .Select(g => g
                .OrderByDescending(k => k.IsUnique)
                .ThenByDescending(k => k.CreateIndex)
                .First());
    }

    private static string GetHintName(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty);
        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' ? c : '_');
        }

        return $"{builder}TableContextBase.g.cs";
    }
}
