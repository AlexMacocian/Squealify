namespace Squealify.Tests.Models;

[Table("accounts")]
public sealed class AccountDbo
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; init; }

    [ColumnUnique]
    [Varchar(64)]
    public required string Name { get; set; }

    [ColumnName("mail")]
    public string? Email { get; set; }
}
