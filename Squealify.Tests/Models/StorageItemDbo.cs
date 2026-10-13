namespace Squealify.Tests.Models;

[Table("storage_items")]
[TableQuery(nameof(CharacterId))]
[TableForeignKey("characters", [nameof(CharacterId)], ["Id"], OnDelete = ForeignKeyAction.Cascade)]
public sealed class StorageItemDbo
{
    [PrimaryKey]
    public long CharacterId { get; init; }

    [PrimaryKey]
    public StorageType StorageType { get; init; }

    [PrimaryKey]
    public ushort SlotNo { get; init; }

    public required string ItemUId { get; set; }
    public uint Num { get; set; }
}
