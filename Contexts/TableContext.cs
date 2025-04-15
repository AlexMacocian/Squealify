using System.Collections.Immutable;

namespace Squealify.Contexts;
public readonly struct TableContext(string tableName, string dboType, FieldContext primaryKey, ImmutableArray<FieldContext> createFieldContexts, ImmutableArray<TableUniqueContext> tableUniques)
{
    public readonly string TableName = tableName;
    public readonly string DboType = dboType;
    public readonly FieldContext PrimaryKey = primaryKey;
    public readonly ImmutableArray<FieldContext> Fields = createFieldContexts;
    public readonly ImmutableArray<TableUniqueContext> TableUniques = tableUniques;
}
