namespace Squealify.Model;

/// <summary>
/// CLR shape of a mapped property. Enums use the kind of their underlying type.
/// </summary>
internal enum ColumnKind
{
    Boolean,
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Single,
    Double,
    Decimal,
    Char,
    String,
    Guid,
    DateTime,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    TimeSpan,
    Bytes,
    Custom
}
