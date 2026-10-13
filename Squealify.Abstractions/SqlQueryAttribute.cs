using System;

namespace Squealify;

/// <summary>
/// Implements a partial method of a <see cref="SqlQueryContext"/> class with the given SQL.
/// </summary>
/// <remarks>
/// <para>Method parameters bind to <c>@name</c> placeholders; a <see cref="System.Threading.CancellationToken"/> parameter is not bound.</para>
/// <para>Statements starting with SELECT, WITH, VALUES, PRAGMA, EXPLAIN or SHOW, or containing RETURNING, are queries; others are commands.</para>
/// <para>Return types (optionally wrapped in <c>ValueTask</c>):</para>
/// <list type="bullet">
/// <item><c>void</c>: executes the statement.</item>
/// <item><c>int</c> for a command: rows affected.</item>
/// <item>A scalar (primitive, enum, string, byte[], Guid, date and time types): first column of the first row. Nullable: null when there is no row.</item>
/// <item>A tuple: columns by position. A class or struct: settable members by column name (case and underscores ignored). Nullable: null when there is no row.</item>
/// <item><c>List&lt;T&gt;</c> of any of the above: all rows.</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SqlQueryAttribute(string sql) : Attribute
{
    public string Sql { get; } = sql;

    /// <summary>
    /// SQL used instead of <see cref="Sql"/> when the context dialect is <see cref="SqlDialect.PostgreSQL"/>.
    /// </summary>
    public string? PostgreSql { get; set; }
}
