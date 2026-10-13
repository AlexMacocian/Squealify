namespace Squealify;

/// <summary>
/// How column names are derived from member names when <see cref="ColumnNameAttribute"/> is absent.
/// </summary>
public enum ColumnNaming
{
    /// <summary>
    /// The member name.
    /// </summary>
    AsDeclared,

    /// <summary>
    /// The member name in snake case: <c>CharacterCommonId</c> becomes <c>character_common_id</c>.
    /// </summary>
    SnakeCase
}
