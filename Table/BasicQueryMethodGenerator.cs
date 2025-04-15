using Squealify.Contexts;
using Sybil;
using System;
using System.Linq;
using System.Text;

namespace Squealify.Table;
public static class BasicQueryMethodGenerator
{
    public static MethodWithSqlStatement CreateInsertStatement(TableContext context)
    {
        var placeHolder = $"Insert{context.DboType}";
        var sql = new StringBuilder();
        sql.AppendLine()
            .AppendLine($"\t\t\tINSERT INTO {context.TableName}")
            .AppendLine($"\t\t\t\t({string.Join(", ", context.Fields.Select(f => f.Name))})")
            .AppendLine("\t\t\tVALUES")
            .AppendLine($"\t\t\t\t({string.Join(", ", context.Fields.Select(GetPlaceholderName))})");

        var methodBuilder = SyntaxBuilder.CreateMethod(Constants.ValueTaskType, Constants.InsertMethodName)
            .WithModifiers($"{Constants.Public} {Constants.Async}")
            .WithParameter(context.DboType, Constants.DboArgumentName)
            .WithParameter(Constants.CancellationTokenType, Constants.CancellationTokenArgument)
            .WithBody(GenerateFullBody(placeHolder, context));

        return new MethodWithSqlStatement(methodBuilder, placeHolder, sql.ToString());
    }

    public static MethodWithSqlStatement CreateUpsertStatement(TableContext context)
    {
        var placeHolder = $"Upsert{context.DboType}";
        var sql = new StringBuilder();
        sql.AppendLine()
            .AppendLine($"\t\t\tINSERT INTO {context.TableName}")
            .AppendLine($"\t\t\t\t({string.Join(", ", context.Fields.Select(f => f.Name))})")
            .AppendLine("\t\t\tVALUES")
            .AppendLine($"\t\t\t\t({string.Join(", ", context.Fields.Select(GetPlaceholderName))})")
            .AppendLine($"\t\t\tON CONFLICT({context.PrimaryKey.Name}) DO UPDATE SET");

        for(var i = 0; i < context.Fields.Length; i++)
        {
            var fieldContext = context.Fields[i];
            if (fieldContext.Name == context.PrimaryKey.Name)
            {
                continue;
            }

            sql.Append($"\t\t\t\t{fieldContext.Name} = excluded.{fieldContext.Name}");
            if (i < context.Fields.Length - 1)
            {
                sql.AppendLine(",");
            }
        }

        sql.AppendLine(";");
        var methodBuilder = SyntaxBuilder.CreateMethod(Constants.ValueTaskType, Constants.UpsertMethodName)
            .WithModifiers($"{Constants.Public} {Constants.Async}")
            .WithParameter(context.DboType, Constants.DboArgumentName)
            .WithParameter(Constants.CancellationTokenType, Constants.CancellationTokenArgument)
            .WithBody(GenerateFullBody(placeHolder, context));

        return new MethodWithSqlStatement(methodBuilder, placeHolder, sql.ToString());
    }

    public static MethodWithSqlStatement CreateUpdateStatement(TableContext context)
    {
        var placeHolder = $"Update{context.DboType}";
        var sql = new StringBuilder();
        sql.AppendLine()
            .AppendLine($"\t\t\tUPDATE {context.TableName}")
            .AppendLine("\t\t\tSET");

        for (var i = 0; i < context.Fields.Length; i++)
        {
            var fieldContext = context.Fields[i];
            if (fieldContext.Name == context.PrimaryKey.Name)
            {
                continue;
            }

            sql.Append($"\t\t\t\t{fieldContext.Name} = {GetPlaceholderName(fieldContext)}");
            if (i < context.Fields.Length - 1)
            {
                sql.AppendLine(",");
            }
        }

        sql.AppendLine()
            .AppendLine("\t\t\tWHERE")
            .AppendLine($"\t\t\t\t{context.PrimaryKey.Name} = {GetPlaceholderName(context.PrimaryKey)};");

        var methodBuilder = SyntaxBuilder.CreateMethod(Constants.ValueTaskType, Constants.UpdateMethodName)
            .WithModifiers($"{Constants.Public} {Constants.Async}")
            .WithParameter(context.DboType, Constants.DboArgumentName)
            .WithParameter(Constants.CancellationTokenType, Constants.CancellationTokenArgument)
            .WithBody(GenerateFullBody(placeHolder, context));

        return new MethodWithSqlStatement(methodBuilder, placeHolder, sql.ToString());
    }

    public static MethodWithSqlStatement CreateDeleteStatement(TableContext context)
    {
        var placeHolder = $"Delete{context.DboType}";
        var sql = new StringBuilder();
        sql.AppendLine()
            .AppendLine($"\t\t\tDELETE FROM {context.TableName}")
            .AppendLine($"\t\t\tWHERE {context.PrimaryKey.Name} = {GetPlaceholderName(context.PrimaryKey)};");

        var bodyBuilder = new StringBuilder(@$"
using var command = this.{Constants.DbConnectionPropertyName}.CreateCommand();
command.CommandText = @""{placeHolder}"";
");

        if (!DataTypeMappings.DataTypeToString.TryGetValue(context.PrimaryKey.Type, out var convertedType))
        {
            throw new InvalidOperationException($"Unable to find conversion for {context.PrimaryKey.Type}");
        }

        (var convertToName, _) = SyntaxParsers.GetConversionNames(context.PrimaryKey.PropertyType, convertedType);
        var valueProvider = context.PrimaryKey.RequiresConversion
            ? $"this.{convertToName}({Constants.PrimaryKeyArgumentName})"
            : $"{Constants.PrimaryKeyArgumentName}";

        bodyBuilder.AppendLine($"command.Parameters.Add(this.{Constants.CreateParameterMethod}(command, \"{GetPlaceholderName(context.PrimaryKey)}\", {valueProvider}));");
        bodyBuilder.AppendLine("await command.ExecuteNonQueryAsync(cancellationToken);");

        var methodBuilder = SyntaxBuilder.CreateMethod(Constants.ValueTaskType, Constants.DeleteMethodName)
            .WithModifiers($"{Constants.Public} {Constants.Async}")
            .WithParameter(context.PrimaryKey.PropertyType, Constants.PrimaryKeyArgumentName)
            .WithParameter(Constants.CancellationTokenType, Constants.CancellationTokenArgument)
            .WithBody(bodyBuilder.ToString());

        return new MethodWithSqlStatement(methodBuilder, placeHolder, sql.ToString());
    }

    public static MethodWithSqlStatement CreateFindStatement(TableContext context)
    {
        var placeHolder = $"Find{context.DboType}";
        var sql = new StringBuilder();
        sql.AppendLine()
            .AppendLine($"\t\t\tSELECT * FROM {context.TableName}")
            .AppendLine($"\t\t\tWHERE {context.PrimaryKey.Name} = {GetPlaceholderName(context.PrimaryKey)};");

        var bodyBuilder = new StringBuilder(@$"
using var command = this.{Constants.DbConnectionPropertyName}.CreateCommand();
command.CommandText = @""{placeHolder}"";
");

        if (!DataTypeMappings.DataTypeToString.TryGetValue(context.PrimaryKey.Type, out var convertedType))
        {
            throw new InvalidOperationException($"Unable to find conversion for {context.PrimaryKey.Type}");
        }

        (var convertToName, _) = SyntaxParsers.GetConversionNames(context.PrimaryKey.PropertyType, convertedType);
        var valueProvider = context.PrimaryKey.RequiresConversion
            ? $"this.{convertToName}({Constants.PrimaryKeyArgumentName})"
            : $"{Constants.PrimaryKeyArgumentName}";

        bodyBuilder.AppendLine($"command.Parameters.Add(this.{Constants.CreateParameterMethod}(command, \"{GetPlaceholderName(context.PrimaryKey)}\", {valueProvider}));");
        bodyBuilder.AppendLine($"using var reader = await command.ExecuteReaderAsync({Constants.CancellationTokenArgument});");
        bodyBuilder.AppendLine($"if (!await reader.ReadAsync({Constants.CancellationTokenArgument})) return default;");
        bodyBuilder.AppendLine($"return new {context.DboType}");
        bodyBuilder.AppendLine("{");
        foreach((var index, var fieldContext) in context.Fields.Select((f, i) => (i, f)))
        {
            if (!DataTypeMappings.DataTypeToString.TryGetValue(fieldContext.Type, out var convertedFieldType))
            {
                continue;
            }

            (_, var convertFromName) = SyntaxParsers.GetConversionNames(fieldContext.PropertyType, convertedFieldType);

            var fieldValueProvider = fieldContext.RequiresConversion
                ? $"this.{convertFromName}({GetReaderMethod("reader", fieldContext)}({index}))"
                : $"{GetReaderMethod("reader", fieldContext)}({index})";

            var nullCheckGuardedProvider = fieldContext.IsNullable
                ? $"await reader.IsDBNullAsync({index}, {Constants.CancellationTokenArgument}) ? default : {fieldValueProvider}"
                : fieldValueProvider;

            bodyBuilder.Append($"{fieldContext.PropertyName} = {nullCheckGuardedProvider}");
            if (index < context.Fields.Length - 1)
            {
                bodyBuilder.AppendLine(",");
            }
        }
        bodyBuilder
            .AppendLine()
            .AppendLine("};");

        var methodBuilder = SyntaxBuilder.CreateMethod($"{Constants.ValueTaskType}<{context.DboType}>", Constants.FindMethodName)
            .WithModifiers($"{Constants.Public} {Constants.Async}")
            .WithParameter(context.PrimaryKey.PropertyType, Constants.PrimaryKeyArgumentName)
            .WithParameter(Constants.CancellationTokenType, Constants.CancellationTokenArgument)
            .WithBody(bodyBuilder.ToString());

        return new MethodWithSqlStatement(methodBuilder, placeHolder, sql.ToString());
    }

    private static string GenerateFullBody(string placeHolder, TableContext context)
    {
        var bodyBuilder = new StringBuilder(@$"
using var command = this.{Constants.DbConnectionPropertyName}.CreateCommand();
command.CommandText = @""{placeHolder}"";
");

        foreach (var fieldContext in context.Fields)
        {
            var propertyType = fieldContext.PropertyType;
            var dataType = fieldContext.Type;
            if (!DataTypeMappings.DataTypeToString.TryGetValue(dataType, out var convertedType))
            {
                continue;
            }

            (var convertToName, _) = SyntaxParsers.GetConversionNames(propertyType, convertedType);
            var valueProvider = fieldContext.RequiresConversion
                ? $"this.{convertToName}({Constants.DboArgumentName}.{fieldContext.PropertyName})"
                : $"{Constants.DboArgumentName}.{fieldContext.PropertyName}";

            bodyBuilder.AppendLine($"command.Parameters.Add(this.{Constants.CreateParameterMethod}(command, \"{GetPlaceholderName(fieldContext)}\", {valueProvider}));");
        }

        bodyBuilder.AppendLine("await command.ExecuteNonQueryAsync(cancellationToken);");
        return bodyBuilder.ToString();
    }

    private static string GetPlaceholderName(FieldContext context) => $"@{context.Name}";

    private static string GetReaderMethod(string readerVariableName, FieldContext context)
    {
        var readMethod = context.Type switch
        {
            DataTypes.SMALLINT => "GetInt16",
            DataTypes.INTEGER => "GetInt32",
            DataTypes.BIGINT => "GetInt64",
            DataTypes.REAL => "GetFloat",
            DataTypes.DOUBLE_PRECISION => "GetDouble",
            DataTypes.DECIMAL => "GetDecimal",
            DataTypes.CHAR => "GetChar",
            DataTypes.VARCHAR => "GetString",
            DataTypes.TEXT => "GetString",
            DataTypes.DATE => "GetDateTime",
            DataTypes.TIME => "GetDateTime",
            DataTypes.TIMESTAMP => "GetDateTime",
            DataTypes.BOOLEAN => "GetBoolean",
            DataTypes.BLOB => "GetBytes",
            _ => throw new NotSupportedException($"Data type {context.Type} is not supported.")
        };

        var readerMethod = context.IsEnum
            ? $"({context.PropertyType}){readerVariableName}.{readMethod}"
            : $"{readerVariableName}.{readMethod}";

        if(context.PropertyType.ToLower().TrimEnd('?') is "datetimeoffset" or "system.datetimeoffset")
        {
            return context.IsNullable
                ? $"(DateTimeOffset?){readerMethod}"
                : $"(DateTimeOffset){readerMethod}";
        }

        return readerMethod;
    }
}
