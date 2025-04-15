using Squealify.Contexts;
using Sybil;

namespace Squealify.Table;
public static class ConversionPropertyGenerator
{
    public static (PropertyBuilder ConvertTo, PropertyBuilder ConvertFrom)? GenerateConversionProperties(FieldContext fieldContext)
    {
        var propertyType = fieldContext.PropertyType;
        var dataType = fieldContext.Type;
        if (!DataTypeMappings.DataTypeToString.TryGetValue(dataType, out var convertedType))
        {
            return default;
        }

        (var convertToName, var convertFromName) = SyntaxParsers.GetConversionNames(propertyType, convertedType);
        var convertTo = SyntaxBuilder.CreateProperty($"{Constants.FuncType}<{propertyType}, {convertedType}>", convertToName)
            .WithModifier(Constants.Protected)
            .WithModifier(Constants.Abstract)
            .WithAccessor(SyntaxBuilder.CreateGetter());

        var convertFrom = SyntaxBuilder.CreateProperty($"{Constants.FuncType}<{convertedType}, {propertyType}>", convertFromName)
            .WithModifier(Constants.Protected)
            .WithModifier(Constants.Abstract)
            .WithAccessor(SyntaxBuilder.CreateGetter());

        return (convertTo, convertFrom);
    }
}
