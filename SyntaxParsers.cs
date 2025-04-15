using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Squealify.Contexts;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Squealify;
public static class SyntaxParsers
{
    public static (string ConvertToName, string ConvertFromName) GetConversionNames(string propertyType, string convertedType)
    {
        var convertedTypeSafeNameBuilder = new StringBuilder(convertedType);
        convertedTypeSafeNameBuilder[0] = char.ToUpper(convertedTypeSafeNameBuilder[0]);
        if (convertedType.Contains("[]"))
        {
            convertedTypeSafeNameBuilder.Replace("[]", Constants.ArrayName);
        }

        convertedTypeSafeNameBuilder.Replace("?", "");
        var convertedTypeSafeName = convertedTypeSafeNameBuilder.ToString();
        var propertyTypeSafeName = propertyType.Replace("?", "").Replace("[]", Constants.ArrayName);

        var convertToName = $"{propertyTypeSafeName}To{convertedTypeSafeName}Converter";
        var convertFromName = $"{convertedTypeSafeName}To{propertyTypeSafeName}Converter";
        return (convertToName, convertFromName);
    }

    public static string GetTableName(ClassDeclarationSyntax classDeclarationSyntax)
    {
        var tableAttribute = classDeclarationSyntax.AttributeLists
            .SelectMany(n => n.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == Constants.TableAttributeName ||
                                 a.Name.ToString() == Constants.TableAttributeShortName);

        if (tableAttribute is null)
        {
            return classDeclarationSyntax.Identifier.ValueText;
        }

        if (tableAttribute.ArgumentList is not null && tableAttribute.ArgumentList.Arguments.Count > 0)
        {
            foreach (var arg in tableAttribute.ArgumentList.Arguments)
            {
                if (arg.NameEquals != null && arg.NameEquals.Name.Identifier.ValueText == Constants.TableNameArgumentName)
                {
                    return arg.Expression.ToString().Trim('"');
                }
            }

            return tableAttribute.ArgumentList.Arguments[0].Expression.ToString().Trim('"');
        }

        return classDeclarationSyntax.Identifier.ValueText;
    }

    public static ImmutableArray<TableUniqueContext> GetTableUniques(ClassDeclarationSyntax classDeclarationSyntax)
    {
        var uniquesBuilder = ImmutableArray.CreateBuilder<TableUniqueContext>();
        var classAttributes = classDeclarationSyntax.AttributeLists.SelectMany(a => a.Attributes);
        foreach (var attribute in classAttributes)
        {
            var name = attribute.Name.ToString();
            if (name == Constants.TableUniqueAttributeName || name == Constants.TableUniqueAttributeShortName)
            {
                if (attribute.ArgumentList is not null)
                {
                    var columnNamesBuilder = ImmutableArray.CreateBuilder<string>();
                    foreach (var arg in attribute.ArgumentList.Arguments)
                    {
                        if (arg.Expression is InvocationExpressionSyntax invocation &&
                            invocation.Expression.ToString() == "nameof")
                        {
                            // e.g. nameof(PropertyName)
                            // The argument's expression typically is an IdentifierNameSyntax
                            if (invocation.ArgumentList.Arguments.Count == 1)
                            {
                                var nameofArg = invocation.ArgumentList.Arguments[0].Expression.ToString();
                                columnNamesBuilder.Add(nameofArg);
                            }
                        }
                        else
                        {
                            var text = arg.Expression.ToString().Trim('"');
                            columnNamesBuilder.Add(text);
                        }
                    }

                    uniquesBuilder.Add(new TableUniqueContext(columnNamesBuilder.ToImmutable()));
                }
            }
        }

        return uniquesBuilder.ToImmutable();
    }

    public static ImmutableArray<FieldContext> GetFieldsFromClass(ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation)
    {
        var fieldsBuilder = ImmutableArray.CreateBuilder<FieldContext>();
        foreach (var member in classDeclarationSyntax.Members)
        {
            if (member is PropertyDeclarationSyntax propertyDeclaration)
            {
                // Ensure the property is public.
                if (!propertyDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    continue;
                }

                // Check that the property has an accessor list containing a getter and a setter or init.
                if (propertyDeclaration.AccessorList is not { } accessorList)
                {
                    continue;
                }

                bool hasGetter = false;
                bool hasSetOrInit = false;
                foreach (var accessor in accessorList.Accessors)
                {
                    if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                    {
                        hasGetter = true;
                    }
                    else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                    {
                        hasSetOrInit = true;
                    }
                }

                if (!hasGetter || !hasSetOrInit)
                {
                    continue;
                }

                // Default field name to property name.
                var fieldName = propertyDeclaration.Identifier.ValueText;
                var originalName = fieldName;
                var isUnique = false;
                var isPrimaryKey = false;
                var isForeignKey = false;
                string? referenceTable = null;
                string? referenceField = null;
                byte? varCharSize = null;

                // Process attributes.
                foreach (var attributeList in propertyDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var attrName = attribute.Name.ToString();
                        // Check for Unique attribute.
                        if (attrName == Constants.ColumnUniqueAttributeName || attrName == Constants.ColumnUniqueAttributeShortName)
                        {
                            isUnique = true;
                            continue;
                        }

                        // Check for PrimaryKey attribute.
                        if (attrName == Constants.PrimaryKeyAttributeName || attrName == Constants.PrimaryKeyAttributeShortName)
                        {
                            isPrimaryKey = true;
                            continue;
                        }

                        // Check for Varchar attribute.
                        if (attrName == Constants.VarcharAttributeName || attrName == Constants.VarcharAttributeShortName)
                        {
                            if (attribute.ArgumentList is not null && attribute.ArgumentList.Arguments.Count >= 1)
                            {
                                // Extract first two arguments as referenceTable and referenceField.
                                varCharSize = byte.Parse(attribute.ArgumentList.Arguments[0].ToString().Trim('"'));
                                continue;
                            }
                        }

                        // Check for ForeignKey attribute.
                        if (attrName == Constants.ForeignKeyAttributeName || attrName == Constants.ForeignKeyAttributeShortName)
                        {
                            isForeignKey = true;
                            if (attribute.ArgumentList is not null && attribute.ArgumentList.Arguments.Count >= 2)
                            {
                                // Extract first two arguments as referenceTable and referenceField.
                                referenceTable = attribute.ArgumentList.Arguments[0].ToString().Trim('"');
                                referenceField = attribute.ArgumentList.Arguments[1].ToString().Trim('"');
                                continue;
                            }
                        }

                        // Check for Name attribute.
                        if (attrName == Constants.ColumnNameAttributeName || attrName == Constants.ColumnNameAttributeShortName)
                        {
                            if (attribute.ArgumentList is not null && attribute.ArgumentList.Arguments.Count >= 1)
                            {
                                fieldName = attribute.ArgumentList.Arguments[0].ToString().Trim('"');
                                continue;
                            }
                        }
                    }
                }

                var semanticModel = compilation.GetSemanticModel(propertyDeclaration.SyntaxTree);
                var typeSymbol = semanticModel.GetTypeInfo(propertyDeclaration.Type).Type;
                var propertyTypeString = propertyDeclaration.Type.ToString();
                var nullable = propertyTypeString.EndsWith("?");
                (var isEnum, var enumUnderlyingType) = IsEnumType(typeSymbol);
                (var propertyMapping, var requiresConversion) = MapPropertyType(propertyTypeString);
                requiresConversion = !isEnum && requiresConversion;
                var dataType = isEnum
                    ? enumUnderlyingType ?? DataTypes.INTEGER
                    : varCharSize is null
                        ? propertyMapping
                        : DataTypes.VARCHAR;
                fieldsBuilder.Add(new FieldContext(
                    fieldName,
                    originalName,
                    propertyTypeString,
                    dataType,
                    isUnique,
                    isPrimaryKey,
                    isForeignKey,
                    referenceTable,
                    referenceField,
                    varCharSize,
                    nullable,
                    isEnum,
                    requiresConversion));
            }
        }

        return fieldsBuilder.ToImmutable();
    }

    private static (bool, DataTypes? UnderlyingType) IsEnumType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            return (false, default);
        }

        if (typeSymbol.TypeKind == TypeKind.Enum && typeSymbol is INamedTypeSymbol enumType)
        {
            return (true, enumType.EnumUnderlyingType?.ToString().ToLowerInvariant() switch
            {
                "byte" or "system.byte" => DataTypes.SMALLINT,
                "int" or "system.int32" or "uint" or "system.uint32" => DataTypes.INTEGER,
                "short" or "system.int16" or "ushort" or "system.uint16" => DataTypes.SMALLINT,
                "long" or "system.int64" or "ulong" or "system.uint64" => DataTypes.BIGINT,
                _ => DataTypes.INTEGER
            });
        }

        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1 &&
            namedType.TypeArguments[0].TypeKind == TypeKind.Enum)
        {
            var enumTypeSymbol = (INamedTypeSymbol)namedType.TypeArguments[0];
            return (true, enumTypeSymbol.EnumUnderlyingType?.ToString().ToLowerInvariant() switch
            {
                "byte" or "system.byte" => DataTypes.SMALLINT,
                "int" or "system.int32" or "uint" or "system.uint32" => DataTypes.INTEGER,
                "short" or "system.int16" or "ushort" or "system.uint16" => DataTypes.SMALLINT,
                "long" or "system.int64" or "ulong" or "system.uint64" => DataTypes.BIGINT,
                _ => DataTypes.INTEGER
            });
        }

        return (false, default);
    }

    private static (DataTypes Type, bool RequiresConversion) MapPropertyType(string propertyType)
    {
        // Normalize the type name for matching.
        var typeName = propertyType.Trim();
        var normalizedName = typeName.ToLowerInvariant().TrimEnd('?');
        if (DataTypeMappings.StringToDataType.TryGetValue(normalizedName, out var dataType))
        {
            return (dataType, false);
        }

        return (DataTypes.TEXT, true);
    }
}
