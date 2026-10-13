using System;

namespace Squealify;

/// <summary>
/// Marks a primary key column. Multiple properties form a composite key, in declaration order.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class PrimaryKeyAttribute : Attribute
{
}

/// <summary>
/// Marks a database generated key. Only valid on a single int or long primary key.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class AutoIncrementAttribute : Attribute
{
}

/// <summary>
/// Overrides the column name (defaults to the property name).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ColumnNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>
/// Adds a UNIQUE constraint to the column and generates a FindBy method returning one row.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ColumnUniqueAttribute : Attribute
{
}

/// <summary>
/// Stores the string column as VARCHAR(length).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class VarcharAttribute(byte length) : Attribute
{
    public byte Length { get; } = length;
}

/// <summary>
/// Single column foreign key.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ForeignKeyAttribute(string referenceTable, string field) : Attribute
{
    public string ReferenceTable { get; } = referenceTable;

    public string ReferenceField { get; } = field;

    public ForeignKeyAction OnDelete { get; set; }
}
