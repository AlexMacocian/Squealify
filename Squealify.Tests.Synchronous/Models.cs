using System.Data.Common;

namespace Squealify.Tests.Synchronous;

/// <summary>
/// Custom base of every generated table context in this assembly ([SquealifyOptions(ContextBase)]).
/// </summary>
public abstract class CountingContext(DbConnection connection, SqlDialect dialect) : SqlQueryContext(connection, dialect)
{
    public int ParametersSet { get; private set; }

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        this.ParametersSet++;
        base.SetParameterValue(parameter, value);
    }
}

[Table("sync_accounts", Naming = ColumnNaming.SnakeCase)]
public struct SyncAccountDbo
{
    [PrimaryKey]
    [AutoIncrement]
    public long Id;

    public string AccountName;

    [ColumnName("mail_address")]
    public string? Email;
}

[Table("sync_items", Naming = ColumnNaming.SnakeCase)]
[TableIndex(nameof(CharacterId))]
public record struct SyncItemDbo
{
    [PrimaryKey]
    public uint CharacterId { get; set; }

    [PrimaryKey]
    public ushort SlotNo { get; set; }

    public string ItemUId { get; set; }

    public uint Num { get; set; }

    public DateTimeOffset? AcquiredAt { get; set; }
}

public sealed class SyncAccountContext(DbConnection connection, SqlDialect dialect) : SyncAccountDboTableContextBase(connection, dialect);

public sealed partial class SyncItemContext(DbConnection connection, SqlDialect dialect) : SyncItemDboTableContextBase(connection, dialect)
{
    [SqlQuery(@"SELECT character_id, slot_no, item_u_id, num, acquired_at FROM sync_items WHERE num >= @minimum ORDER BY character_id, slot_no")]
    public partial List<SyncItemDbo> FindWithAtLeast(uint minimum);

    [SqlQuery(@"SELECT COALESCE(SUM(num), 0) FROM sync_items WHERE character_id = @characterId")]
    public partial long SumNum(uint characterId);
}
