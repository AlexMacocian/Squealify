using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;

namespace Squealify.Tests.Synchronous;

public abstract class SynchronousContextTestsBase
{
    private DbConnection? connection;

    protected abstract SqlDialect Dialect { get; }

    protected abstract DbConnection OpenConnection();

    protected virtual void DropTables(DbConnection connection)
    {
    }

    [TestInitialize]
    public void Initialize()
    {
        this.connection = this.OpenConnection();
        this.DropTables(this.connection);
        using var accounts = this.Accounts();
        accounts.CreateTableIfNotExists();
        using var items = this.Items();
        items.CreateTableIfNotExists();
    }

    [TestCleanup]
    public void Cleanup() => this.connection?.Dispose();

    [TestMethod]
    public void StructTable_AutoIncrementAndFieldColumns()
    {
        using var accounts = this.Accounts();
        var account = new SyncAccountDbo { AccountName = "sync", Email = "sync@example.com" };

        var id = accounts.Insert(in account);
        var found = accounts.Find(id);

        Assert.IsNotNull(found);
        Assert.AreEqual("sync", found.Value.AccountName);
        Assert.AreEqual("sync@example.com", found.Value.Email);
        Assert.IsNull(accounts.Find(id + 1000));
    }

    [TestMethod]
    public void SnakeCaseNaming_DerivesColumnNames()
    {
        using var accounts = this.Accounts();
        var account = new SyncAccountDbo { AccountName = "snake", Email = null };
        accounts.Insert(in account);
        using var command = this.connection!.CreateCommand();
        command.CommandText = "SELECT id, account_name, mail_address FROM sync_accounts";

        using var reader = command.ExecuteReader();

        Assert.IsTrue(reader.Read());
        Assert.AreEqual("snake", reader.GetString(1));
        Assert.IsTrue(reader.IsDBNull(2));
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(9)]
    [DataRow(150)]
    public void SpanWrites_AllBatchSizes(int count)
    {
        using var items = this.Items();
        var rows = Enumerable.Range(0, count)
            .Select(i => new SyncItemDbo { CharacterId = 7, SlotNo = (ushort)i, ItemUId = $"item{i}", Num = (uint)i, AcquiredAt = i % 2 == 0 ? null : DateTimeOffset.UnixEpoch.AddDays(i) })
            .ToArray();

        items.InsertMany(rows);
        for (var i = 0; i < rows.Length; i++)
        {
            rows[i].Num += 1000;
        }

        items.UpsertMany(rows);
        var found = items.FindByCharacterId(7);
        var filled = new List<SyncItemDbo>();
        items.FindByCharacterId(7, filled);

        Assert.HasCount(count, found);
        Assert.HasCount(count, filled);
        CollectionAssert.AreEquivalent(rows.Select(r => r.Num).ToArray(), found.Select(r => r.Num).ToArray());
        Assert.AreEqual(count, items.DeleteMany(rows));
        Assert.IsEmpty(items.FindAll());
    }

    [TestMethod]
    public void SingleRowWrites_PassStructsByReference()
    {
        using var items = this.Items();
        var item = new SyncItemDbo { CharacterId = 1, SlotNo = 2, ItemUId = "a", Num = 3 };

        items.Insert(in item);
        Assert.IsFalse(items.InsertIfNotExists(in item));
        item.Num = 4;
        Assert.AreEqual(1, items.Update(in item));
        var other = item with { SlotNo = 3, Num = 5 };
        Assert.IsTrue(items.InsertIfNotExists(in other));
        Assert.AreEqual(2, items.InsertIfNotExistsMany([item with { SlotNo = 10 }, item with { SlotNo = 11 }, item]));

        Assert.AreEqual(4u, items.Find(1, 2)!.Value.Num);
        Assert.IsGreaterThan(0, items.ParametersSet);
        Assert.AreEqual(4L + 5 + 4 + 4, items.SumNum(1));
        Assert.HasCount(1, items.FindWithAtLeast(5));
        Assert.AreEqual(1, items.Delete(1, 2));
        Assert.AreEqual(3, items.DeleteByCharacterId(1));
        Assert.AreEqual(0L, items.SumNum(1));
    }

    protected SyncAccountContext Accounts() => new(this.connection!, this.Dialect);

    protected SyncItemContext Items() => new(this.connection!, this.Dialect);
}

[TestClass]
public sealed class SqliteSynchronousContextTests : SynchronousContextTestsBase
{
    protected override SqlDialect Dialect => SqlDialect.SQLite;

    protected override DbConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }
}

/// <summary>
/// Runs when SQUEALIFY_POSTGRESQL contains a connection string to a disposable database.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PostgreSqlSynchronousContextTests : SynchronousContextTestsBase
{
    private const string ConnectionStringVariable = "SQUEALIFY_POSTGRESQL";

    protected override SqlDialect Dialect => SqlDialect.PostgreSQL;

    protected override DbConnection OpenConnection()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set");
        }

        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    protected override void DropTables(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"DROP TABLE IF EXISTS ""sync_items"", ""sync_accounts"" CASCADE;";
        command.ExecuteNonQuery();
    }
}
