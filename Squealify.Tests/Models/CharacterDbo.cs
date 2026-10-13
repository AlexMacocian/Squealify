namespace Squealify.Tests.Models;

[Table("characters")]
[TableIndex(nameof(AccountId), nameof(GameMode))]
[TableUnique(nameof(FirstName), nameof(LastName))]
public sealed class CharacterDbo
{
    [PrimaryKey]
    [AutoIncrement]
    public long Id { get; init; }

    [ForeignKey("accounts", "Id", OnDelete = ForeignKeyAction.Cascade)]
    public int AccountId { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int GameMode { get; set; }
}
