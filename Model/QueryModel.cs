namespace Squealify.Model;

internal enum QueryShape
{
    /// <summary>
    /// Executes the statement, no result.
    /// </summary>
    Execute,

    /// <summary>
    /// Command returning the number of rows affected.
    /// </summary>
    Affected,

    /// <summary>
    /// First row (or first column of it for scalars).
    /// </summary>
    Single,

    /// <summary>
    /// All rows in a List.
    /// </summary>
    List
}

internal enum RowKind
{
    Scalar,
    Tuple,
    Object
}

/// <summary>
/// How a result row is materialized. <see cref="Members"/> holds one value per scalar/tuple element/object member;
/// for objects <see cref="ColumnModel.ColumnName"/> is the normalized column name matched against the result.
/// </summary>
/// <see cref="Declared"/> is the row type as declared (with nullable annotations), <see cref="Type"/> the constructed type.
internal sealed record RowModel(RowKind Kind, string Type, string Declared, EquatableArray<ColumnModel> Members);

internal sealed record QueryParameterModel(string Name, string Type, bool IsCancellationToken, ColumnModel? Value);

internal sealed record QueryMethodModel(
    string Namespace,
    string ClassName,
    string Name,
    string Accessibility,
    string ReturnType,
    bool IsAsync,
    QueryShape Shape,
    bool NullableResult,
    RowModel? Row,
    EquatableArray<QueryParameterModel> Parameters,
    string Sql,
    string? PostgreSql,
    EquatableArray<DiagnosticModel> Diagnostics);
