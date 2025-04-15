using System.Collections.Generic;

namespace Squealify;
public static class DataTypeMappings
{
    public static readonly Dictionary<string, DataTypes> StringToDataType = new() {
        { "int", DataTypes.INTEGER },
        { "system.int32", DataTypes.INTEGER },
        { "uint", DataTypes.INTEGER },
        { "system.uint32", DataTypes.INTEGER },

        { "short", DataTypes.SMALLINT },
        { "system.int16", DataTypes.SMALLINT },
        { "ushort", DataTypes.SMALLINT },
        { "system.uint16", DataTypes.SMALLINT },

        { "long", DataTypes.BIGINT },
        { "system.int64", DataTypes.BIGINT },
        { "ulong", DataTypes.BIGINT },
        { "system.uint64", DataTypes.BIGINT },

        { "float", DataTypes.REAL },
        { "system.single", DataTypes.REAL },

        { "double", DataTypes.DOUBLE_PRECISION },
        { "system.double", DataTypes.DOUBLE_PRECISION },

        { "decimal", DataTypes.DECIMAL },
        { "system.decimal", DataTypes.DECIMAL },

        { "string", DataTypes.TEXT },
        { "system.string", DataTypes.TEXT },

        { "char", DataTypes.CHAR },
        { "system.char", DataTypes.CHAR },

        { "bool", DataTypes.BOOLEAN },
        { "system.boolean", DataTypes.BOOLEAN },
        { "boolean", DataTypes.BOOLEAN },

        { "datetime", DataTypes.TIMESTAMP },
        { "system.datetime", DataTypes.TIMESTAMP },
        { "datetimeoffset", DataTypes.TIMESTAMP },
        { "system.datetimeoffset", DataTypes.TIMESTAMP },

        { "dateonly", DataTypes.DATE },
        { "system.dateonly", DataTypes.DATE },

        { "timeonly", DataTypes.TIME },
        { "system.timeonly", DataTypes.TIME },

        { "byte[]", DataTypes.BLOB },
        { "system.byte[]", DataTypes.BLOB },
    };

    public static readonly Dictionary<DataTypes, string> DataTypeToString = new()
    {
        { DataTypes.SMALLINT, "short" },
        { DataTypes.INTEGER, "int" },
        { DataTypes.BIGINT, "long" },
        { DataTypes.REAL, "float" },
        { DataTypes.DOUBLE_PRECISION, "double" },
        { DataTypes.DECIMAL, "decimal" },
        { DataTypes.CHAR, "char" },
        { DataTypes.VARCHAR, "string" },
        { DataTypes.TEXT, "string" },
        { DataTypes.DATE, "DateOnly" },
        { DataTypes.TIME, "TimeOnly" },
        { DataTypes.TIMESTAMP, "DateTimeOffset" },
        { DataTypes.BOOLEAN, "bool" },
        { DataTypes.BLOB, "byte[]" }
    };
}
