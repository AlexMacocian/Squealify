using System.Collections.Immutable;

namespace Squealify.Contexts;
public readonly struct TableUniqueContext(ImmutableArray<string> columnNames)
{
    public readonly ImmutableArray<string> ColumnNames = columnNames;
}
