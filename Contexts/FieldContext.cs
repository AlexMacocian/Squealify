namespace Squealify.Contexts;

public readonly struct FieldContext(
    string name,
    string propertyName,
    string propertyType,
    DataTypes type,
    bool isUnique,
    bool isPrimaryKey,
    bool isForeignKey,
    string? referenceTable,
    string? referenceField,
    byte? varcharLength,
    bool nullable,
    bool isEnum,
    bool requiresConversion)
{
    public readonly string Name = name;
    public readonly string PropertyName = propertyName;
    public readonly string PropertyType = propertyType;
    public readonly DataTypes Type = type;
    public readonly bool IsUnique = isUnique;
    public readonly bool IsPrimaryKey = isPrimaryKey;
    public readonly bool IsForeignKey = isForeignKey;
    public readonly bool IsNullable = nullable;
    public readonly bool IsEnum = isEnum;
    public readonly string? ReferenceTable = referenceTable;
    public readonly string? ReferenceField = referenceField;
    public readonly byte? VarcharLength = varcharLength;
    public readonly bool RequiresConversion = requiresConversion;
}
