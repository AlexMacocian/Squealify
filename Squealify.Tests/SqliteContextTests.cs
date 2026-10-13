using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Squealify.Tests;

[TestClass]
public sealed class SqliteContextTests : ContextTestsBase
{
    protected override SqlDialect Dialect => SqlDialect.SQLite;

    protected override async Task<DbConnection> OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync();
        return connection;
    }
}
