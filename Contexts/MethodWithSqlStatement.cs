using Sybil;

namespace Squealify.Contexts;
public readonly struct MethodWithSqlStatement(MethodBuilder methodBuilder, string placeholder, string commandText)
{
    public readonly MethodBuilder MethodBuilder = methodBuilder;
    public readonly string Placeholder = placeholder;
    public readonly string CommandText = commandText;
}
