using System;

namespace Squealify;

/// <summary>
/// UNIQUE constraint over the given columns; generates a FindBy method returning one row.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class TableUniqueAttribute(params string[] uniqueFields) : Attribute
{
    public string[] Uniques { get; } = uniqueFields;
}

/// <summary>
/// Index over the given columns; generates FindBy (all matching rows) and DeleteBy methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class TableIndexAttribute(params string[] columns) : Attribute
{
    public string[] Columns { get; } = columns;
}

/// <summary>
/// Generates FindBy and DeleteBy methods over the given columns without creating an index.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class TableQueryAttribute(params string[] columns) : Attribute
{
    public string[] Columns { get; } = columns;
}

/// <summary>
/// Composite foreign key.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class TableForeignKeyAttribute(string referenceTable, string[] columns, string[] referenceColumns) : Attribute
{
    public string ReferenceTable { get; } = referenceTable;

    public string[] Columns { get; } = columns;

    public string[] ReferenceColumns { get; } = referenceColumns;

    public ForeignKeyAction OnDelete { get; set; }
}
