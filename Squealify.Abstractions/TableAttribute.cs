using System;

namespace Squealify;

/// <summary>
/// Marks a class or struct as a table. Squealify generates a {Type}TableContextBase for it.
/// Columns are the public instance properties with a getter and a setter, and the public mutable instance fields.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute : Attribute
{
    public TableAttribute()
    {
        this.Name = string.Empty;
    }

    public TableAttribute(string name)
    {
        this.Name = name;
    }

    public string Name { get; }

    /// <summary>
    /// Column names of members without <see cref="ColumnNameAttribute"/>.
    /// </summary>
    public ColumnNaming Naming { get; set; }
}
