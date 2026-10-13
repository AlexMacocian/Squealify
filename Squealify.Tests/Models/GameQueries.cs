using System.Data.Common;

namespace Squealify.Tests.Models;

public sealed class CharacterSummary
{
    public long Id { get; set; }
    public required string FirstName { get; set; }
    public string? AccountName { get; set; }

    [ColumnName("mail")]
    public string? Email { get; set; }
}

public struct StorageSlot
{
    public StorageType StorageType;
    public ushort SlotNo;
    public uint Num;
}

/// <summary>
/// One [SqlQuery] method per supported shape.
/// </summary>
public sealed partial class GameQueries(DbConnection connection, SqlDialect dialect) : SqlQueryContext(connection, dialect)
{
    [SqlQuery(@"SELECT COUNT(*) FROM ""characters"" WHERE ""AccountId"" = @accountId")]
    public partial long CountCharacters(int accountId);

    [SqlQuery(@"
        SELECT c.""Id"", c.""FirstName"", a.""Name"" AS account_name, a.""mail""
        FROM ""characters"" c
        JOIN ""accounts"" a ON a.""Id"" = c.""AccountId""
        WHERE c.""AccountId"" = @accountId -- @ignored in comments
        ORDER BY c.""Id""")]
    public partial List<CharacterSummary> FindSummaries(int accountId);

    [SqlQuery(@"
        SELECT c.""Id"", c.""FirstName"", a.""Name"" AS account_name, a.""mail""
        FROM ""characters"" c
        JOIN ""accounts"" a ON a.""Id"" = c.""AccountId""
        WHERE c.""AccountId"" = @accountId
        ORDER BY c.""Id""")]
    public partial ValueTask<List<CharacterSummary>> FindSummariesAsync(int accountId, CancellationToken cancellationToken);

    [SqlQuery(@"SELECT c.""Id"", c.""FirstName"", NULL AS account_name, '@notAParameter' AS mail FROM ""characters"" c WHERE c.""Id"" = @id")]
    public partial CharacterSummary? FindSummary(long id);

    [SqlQuery(@"SELECT ""FirstName"" FROM ""characters"" WHERE ""Id"" = @id")]
    public partial string? FindFirstName(long id);

    [SqlQuery(@"SELECT ""Name"" FROM ""accounts"" WHERE ""Id"" = @id", PostgreSql = @"SELECT ""Name"" FROM ""accounts"" WHERE ""Id"" = @id LIMIT 1")]
    public partial string GetAccountName(int id);

    [SqlQuery(@"SELECT ""StorageType"", ""SlotNo"", ""Num"" FROM ""storage_items"" WHERE ""CharacterId"" = @characterId ORDER BY ""SlotNo""")]
    public partial List<StorageSlot> FindSlots(long characterId);

    [SqlQuery(@"SELECT ""StorageType"", ""SlotNo"" FROM ""storage_items"" WHERE ""CharacterId"" = @characterId AND ""SlotNo"" = @slotNo")]
    public partial (StorageType Type, ushort Slot)? FindSlot(long characterId, ushort slotNo);

    [SqlQuery(@"SELECT ""Id"" FROM ""characters"" ORDER BY ""Id""")]
    public partial List<long> AllCharacterIds();

    [SqlQuery(@"SELECT EXISTS (SELECT 1 FROM ""characters"" WHERE ""Id"" = @id)")]
    public partial bool CharacterExists(long id);

    [SqlQuery(@"UPDATE ""storage_items"" SET ""Num"" = ""Num"" + @amount WHERE ""CharacterId"" = @characterId")]
    public partial int AddToAll(long characterId, uint amount);

    [SqlQuery(@"DELETE FROM ""storage_items"" WHERE ""CharacterId"" = @characterId")]
    public partial ValueTask ClearAsync(long characterId, CancellationToken cancellationToken);

    [SqlQuery(@"INSERT INTO ""accounts"" (""Name"", ""mail"") VALUES (@name, @email) RETURNING ""Id""")]
    public partial int InsertAccount(string name, string? email);

    protected override DbParameter CreateParameter<TValue>(DbCommand command, string name)
        => NpgsqlTypedParameters.Create<TValue>(command, name) ?? base.CreateParameter<TValue>(command, name);

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        if (!NpgsqlTypedParameters.TrySet(parameter, value))
        {
            base.SetParameterValue(parameter, value);
        }
    }
}
