using Npgsql;
using System.Data.Common;

namespace Squealify.Tests;

/// <summary>
/// Runs when SQUEALIFY_POSTGRESQL contains a connection string to a disposable database.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PostgreSqlContextTests : ContextTestsBase
{
    private const string ConnectionStringVariable = "SQUEALIFY_POSTGRESQL";

    protected override SqlDialect Dialect => SqlDialect.PostgreSQL;

    protected override async Task<DbConnection> OpenConnection()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set");
        }

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    protected override async Task DropTables(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"DROP TABLE IF EXISTS ""sequences"", ""all_types"", ""unlocks"", ""crests"", ""storage_items"", ""characters"", ""accounts"" CASCADE;";
        await command.ExecuteNonQueryAsync();
    }
}
