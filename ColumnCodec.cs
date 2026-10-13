using Squealify.Model;
using System;

namespace Squealify;

/// <summary>
/// Conversions between CLR values and database parameters and readers. Shared by table contexts and queries, so both use the same storage types.
/// </summary>
internal static class ColumnCodec
{
    /// <summary>
    /// CLR type stored in the database parameter (after unsigned/enum/custom conversion).
    /// </summary>
    public static string ParameterType(ColumnModel column)
    {
        var type = column.Kind switch
        {
            ColumnKind.Boolean => "bool",
            ColumnKind.Byte or ColumnKind.SByte or ColumnKind.Int16 => "short",
            ColumnKind.UInt16 or ColumnKind.Int32 => "int",
            ColumnKind.UInt32 or ColumnKind.Int64 or ColumnKind.UInt64 => "long",
            ColumnKind.Single => "float",
            ColumnKind.Double => "double",
            ColumnKind.Decimal => "decimal",
            ColumnKind.Guid => "Guid",
            ColumnKind.DateTime => "DateTime",
            ColumnKind.DateTimeOffset => "DateTimeOffset",
            ColumnKind.DateOnly => "DateOnly",
            ColumnKind.TimeOnly => "TimeOnly",
            ColumnKind.TimeSpan => "TimeSpan",
            ColumnKind.Bytes => "byte[]",
            _ => "string"
        };

        return column.IsNullable ? type + "?" : type;
    }

    /// <summary>
    /// Expression converting a property value to its parameter type.
    /// </summary>
    public static string WriteExpression(ColumnModel column, string value)
    {
        if (!column.IsNullable)
        {
            return ConvertValue(column, value);
        }

        if (column.IsValueType)
        {
            var converted = ConvertValue(column, $"{value}.Value");
            return converted == $"{value}.Value"
                ? value
                : $"{value}.HasValue ? ({ParameterType(column)}){converted} : null";
        }

        var convertedReference = ConvertValue(column, value);
        return convertedReference == value
            ? value
            : $"{value} is null ? null : {convertedReference}";
    }

    /// <summary>
    /// Expression reading the column at <paramref name="ordinal"/> (an expression) from <c>reader</c>.
    /// </summary>
    public static string ReadExpression(ColumnModel column, string ordinal)
    {
        var read = column.Kind switch
        {
            ColumnKind.Boolean => $"reader.GetBoolean({ordinal})",
            ColumnKind.Byte => $"(byte)reader.GetInt16({ordinal})",
            ColumnKind.SByte => $"(sbyte)reader.GetInt16({ordinal})",
            ColumnKind.Int16 => $"reader.GetInt16({ordinal})",
            ColumnKind.UInt16 => $"(ushort)reader.GetInt32({ordinal})",
            ColumnKind.Int32 => $"reader.GetInt32({ordinal})",
            ColumnKind.UInt32 => $"(uint)reader.GetInt64({ordinal})",
            ColumnKind.Int64 => $"reader.GetInt64({ordinal})",
            ColumnKind.UInt64 => $"unchecked((ulong)reader.GetInt64({ordinal}))",
            ColumnKind.Single => $"reader.GetFloat({ordinal})",
            ColumnKind.Double => $"reader.GetDouble({ordinal})",
            ColumnKind.Decimal => $"reader.GetDecimal({ordinal})",
            ColumnKind.Char => $"reader.GetString({ordinal})[0]",
            ColumnKind.String => $"reader.GetString({ordinal})",
            ColumnKind.Guid => $"reader.GetGuid({ordinal})",
            ColumnKind.DateTime => $"reader.GetDateTime({ordinal})",
            ColumnKind.DateTimeOffset => $"reader.GetFieldValue<DateTimeOffset>({ordinal})",
            ColumnKind.DateOnly => $"reader.GetFieldValue<DateOnly>({ordinal})",
            ColumnKind.TimeOnly => $"reader.GetFieldValue<TimeOnly>({ordinal})",
            ColumnKind.TimeSpan => $"reader.GetFieldValue<TimeSpan>({ordinal})",
            ColumnKind.Bytes => $"reader.GetFieldValue<byte[]>({ordinal})",
            _ => $"this.{column.ConvertFromName}(reader.GetString({ordinal}))"
        };

        if (column.EnumType is not null)
        {
            read = $"({column.EnumType})({read})";
        }

        if (!column.IsNullable)
        {
            return read;
        }

        return column.IsValueType
            ? $"reader.IsDBNull({ordinal}) ? null : ({column.ValueType}?)({read})"
            : $"reader.IsDBNull({ordinal}) ? null : {read}";
    }

    private static string UnderlyingType(ColumnKind kind) => kind switch
    {
        ColumnKind.Byte => "byte",
        ColumnKind.SByte => "sbyte",
        ColumnKind.Int16 => "short",
        ColumnKind.UInt16 => "ushort",
        ColumnKind.Int32 => "int",
        ColumnKind.UInt32 => "uint",
        ColumnKind.Int64 => "long",
        ColumnKind.UInt64 => "ulong",
        _ => throw new InvalidOperationException($"{kind} cannot be an enum underlying type")
    };

    private static string ConvertValue(ColumnModel column, string value)
    {
        var inner = column.EnumType is null ? value : $"({UnderlyingType(column.Kind)}){value}";
        return column.Kind switch
        {
            ColumnKind.Byte or ColumnKind.SByte => $"(short){inner}",
            ColumnKind.UInt16 => $"(int){inner}",
            ColumnKind.UInt32 => $"(long){inner}",
            ColumnKind.UInt64 => $"unchecked((long){inner})",
            ColumnKind.Char => $"{value}.ToString()",
            ColumnKind.DateTimeOffset => $"{value}.ToUniversalTime()",
            ColumnKind.Custom => $"this.{column.ConvertToName}({value})",
            _ => inner
        };
    }
}
