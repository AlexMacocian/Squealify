namespace Squealify;

/// <summary>
/// Referential action applied to dependent rows when the referenced row is deleted.
/// </summary>
public enum ForeignKeyAction
{
    NoAction = 0,
    Cascade = 1,
    SetNull = 2,
    Restrict = 3,
    SetDefault = 4
}
