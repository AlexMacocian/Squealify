namespace Squealify.Tests.Models;

[Table("unlocks")]
public sealed class UnlockDbo
{
    [PrimaryKey]
    public long CharacterId { get; init; }

    [PrimaryKey]
    public int Category { get; init; }

    [PrimaryKey]
    public uint Value { get; init; }
}
