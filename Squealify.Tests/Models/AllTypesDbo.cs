namespace Squealify.Tests.Models;

[Table("all_types")]
public sealed class AllTypesDbo
{
    [PrimaryKey]
    public required Guid Id { get; init; }
    public bool Bool { get; set; }
    public byte Byte { get; set; }
    public sbyte SByte { get; set; }
    public short Short { get; set; }
    public ushort UShort { get; set; }
    public int Int { get; set; }
    public uint UInt { get; set; }
    public long Long { get; set; }
    public ulong ULong { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public decimal Decimal { get; set; }
    public char Char { get; set; }
    public required string String { get; set; }
    public DateTime DateTime { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
    public DateOnly DateOnly { get; set; }
    public TimeOnly TimeOnly { get; set; }
    public TimeSpan TimeSpan { get; set; }
    public required byte[] Bytes { get; set; }
    public ByteEnum ByteEnum { get; set; }
    public UIntEnum UIntEnum { get; set; }
    public required CustomId Custom { get; set; }

    public int? NullableInt { get; set; }
    public uint? NullableUInt { get; set; }
    public Guid? NullableGuid { get; set; }
    public string? NullableString { get; set; }
    public DateTimeOffset? NullableDateTimeOffset { get; set; }
    public byte[]? NullableBytes { get; set; }
    public UIntEnum? NullableUIntEnum { get; set; }
    public CustomId? NullableCustom { get; set; }

    public string Computed => this.String + "!";
}
