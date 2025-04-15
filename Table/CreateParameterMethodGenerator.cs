using Sybil;

namespace Squealify.Table;
public static class CreateParameterMethodGenerator
{
    public static MethodBuilder CreateParameterMethod()
    {
        var methodBuilder = SyntaxBuilder.CreateMethod(Constants.DbParameterType, Constants.CreateParameterMethod)
            .WithModifier(Constants.Protected)
            .WithParameter(Constants.DbCommandType, Constants.CreateParameterCommandArgument)
            .WithParameter(Constants.StringType, Constants.CreateParameterNameArgument)
            .WithParameter(Constants.NullableObject, Constants.CreateParameterValueArgument)
            .WithBody($@"
var parameter = {Constants.CreateParameterCommandArgument}.CreateParameter();
parameter.ParameterName = {Constants.CreateParameterNameArgument};
parameter.Value = {Constants.CreateParameterValueArgument} ?? DBNull.Value;
return parameter;");

        return methodBuilder;
    }
}
