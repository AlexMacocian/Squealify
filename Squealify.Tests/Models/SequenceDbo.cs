namespace Squealify.Tests.Models;

[Table("sequences")]
public sealed class SequenceDbo
{
    [PrimaryKey]
    [AutoIncrement]
    public long Id { get; init; }
}
