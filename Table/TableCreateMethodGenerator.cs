using Squealify.Contexts;
using Sybil;
using System.Text;

namespace Squealify.Table;
public static class TableCreateMethodGenerator
{
    public static (MethodWithSqlStatement Create, MethodWithSqlStatement CreateIfNotExists) GenerateCreateMethods(TableContext ctx)
    {
        (var createMethod, var createMethodPlaceholder, var createMethodBody) =
            GenerateCreate(Constants.CreateTableStatement, Constants.CreateTableMethod, ctx);
        (var createIfNotExistsMethod, var createIfNotExistsPlacholder, var createIfNotExistsBody) =
            GenerateCreate(Constants.CreateTableIfNotExistsStatement, Constants.CreateTableIfNotExistsMethod, ctx);

        return (
            new MethodWithSqlStatement(createMethod, createMethodPlaceholder, createMethodBody),
            new MethodWithSqlStatement(createIfNotExistsMethod, createIfNotExistsPlacholder, createIfNotExistsBody));
    }

    /// <summary>
    /// Generates the create method. Generates the CommandText body but puts it inside a placeholder, to avoid auto-formatting by Roslyn.
    /// </summary>
    private static (MethodBuilder Method, string Placeholder, string CommandText) GenerateCreate(string createStatement, string createName, TableContext ctx)
    {
        var commandPlaceholder = $"__COMMAND_TEXT_{createName}__";
        var commandTextBuilder = new StringBuilder();
        commandTextBuilder.AppendLine().AppendLine($"\t\t\t{createStatement} {ctx.TableName} (");

        //Generate normal, primary and unique fields
        for (var i = 0; i < ctx.Fields.Length; i++)
        {
            var field = ctx.Fields[i];
            commandTextBuilder.Append($"\t\t\t\t{field.Name} {field.Type.ToString().Replace('_', ' ')}");
            if (field.IsPrimaryKey)
            {
                commandTextBuilder.Append(' ').Append(Constants.PrimaryKey);
            }

            if (field.IsUnique)
            {
                commandTextBuilder.Append(' ').Append(Constants.Unique);
            }

            if (!field.IsPrimaryKey &&
                !field.IsNullable)
            {
                commandTextBuilder.Append(' ').Append(Constants.NotNull);
            }

            if (field.IsForeignKey &&
                field.ReferenceTable is string referenceTable &&
                field.ReferenceField is string referenceField)
            {
                commandTextBuilder.Append(' ').Append($"{Constants.References} {referenceTable}({referenceField})");
            }

            if (i < ctx.Fields.Length - 1)
            {
                commandTextBuilder.AppendLine(",");
            }
        }

        if (ctx.TableUniques.Length > 0)
        {
            commandTextBuilder.Append(',').AppendLine();
        }

        for (var i = 0; i < ctx.TableUniques.Length; i++)
        {
            var tableUnique = ctx.TableUniques[i];
            commandTextBuilder.Append($"\t\t\t\t{Constants.Unique} ({string.Join(", ", tableUnique.ColumnNames)})");
            if (i < ctx.TableUniques.Length - 1)
            {
                commandTextBuilder.AppendLine(",");
            }
        }

        commandTextBuilder.AppendLine(");");
        var methodBuilder = SyntaxBuilder.CreateMethod(Constants.ValueTaskType, createName)
            .WithModifier(Constants.Public)
            .WithModifier(Constants.Async)
            .WithParameter(Constants.CancellationTokenType, Constants.CancellationTokenArgument)
            .WithBody($@"
            using var command = this.{Constants.DbConnectionPropertyName}.CreateCommand();
            command.CommandText = @""{commandPlaceholder}"";
            await command.ExecuteNonQueryAsync({Constants.CancellationTokenArgument});");
        return (methodBuilder, commandPlaceholder, commandTextBuilder.ToString());
    }
}
