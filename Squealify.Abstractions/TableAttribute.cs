using System;

namespace Squealify;

/// <summary>
/// Marks a class as a table. Squealify generates a {Class}TableContextBase for it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
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
}
