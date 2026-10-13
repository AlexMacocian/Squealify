using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Squealify.Model;

internal sealed record ColumnModel(
    string ColumnName,
    string PropertyName,
    string ValueType,
    ColumnKind Kind,
    string? EnumType,
    bool IsNullable,
    bool IsValueType,
    bool IsPrimaryKey,
    bool IsAutoIncrement,
    bool IsUnique,
    int? VarcharLength,
    string? ReferenceTable,
    string? ReferenceColumn,
    int OnDelete,
    string? ConvertToName,
    string? ConvertFromName);

/// <summary>
/// A set of columns that gets FindBy/DeleteBy methods and optionally an index.
/// </summary>
internal sealed record KeySetModel(
    EquatableArray<int> Columns,
    bool IsUnique,
    bool CreateIndex);

internal sealed record ForeignKeyModel(
    EquatableArray<int> Columns,
    string ReferenceTable,
    EquatableArray<string> ReferenceColumns,
    int OnDelete);

internal sealed record LocationModel(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(this.FilePath, this.Span, this.LineSpan);

    public static LocationModel? FromLocation(Location? location)
    {
        if (location is null || location.SourceTree is null)
        {
            return null;
        }

        return new LocationModel(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}

internal sealed record DiagnosticModel(string Id, EquatableArray<string> Arguments, LocationModel? Location);

internal sealed record TableModel(
    string HintName,
    string ContextName,
    string DboType,
    string TableName,
    EquatableArray<ColumnModel> Columns,
    EquatableArray<KeySetModel> KeySets,
    EquatableArray<ForeignKeyModel> ForeignKeys,
    EquatableArray<DiagnosticModel> Diagnostics);

internal sealed record GenerationOptions(bool FileScopedNamespace);
