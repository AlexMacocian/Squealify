using System;

namespace Squealify;

/// <summary>
/// Assembly wide generation options.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class SquealifyOptionsAttribute : Attribute
{
    /// <summary>
    /// Generates synchronous table context methods (no tasks, struct rows passed by reference, multi-row writes over spans)
    /// instead of asynchronous ones.
    /// </summary>
    public bool Synchronous { get; set; }

    /// <summary>
    /// Base class of generated table contexts instead of <see cref="SqlQueryContext"/>. Must derive from
    /// <see cref="SqlQueryContext"/> and have an accessible <c>(DbConnection, SqlDialect)</c> constructor.
    /// </summary>
    public Type? ContextBase { get; set; }
}
