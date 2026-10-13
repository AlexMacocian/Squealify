namespace Squealify.Tests.Models;

[Table("crests")]
[TableForeignKey("storage_items", [nameof(CharacterId), nameof(StorageType), nameof(SlotNo)], ["CharacterId", "StorageType", "SlotNo"], OnDelete = ForeignKeyAction.Cascade)]
public sealed class CrestDbo
{
    [PrimaryKey]
    public long CharacterId { get; init; }

    [PrimaryKey]
    public StorageType StorageType { get; init; }

    [PrimaryKey]
    public ushort SlotNo { get; init; }

    [PrimaryKey]
    public byte CrestSlot { get; init; }

    public uint CrestId { get; set; }
}
