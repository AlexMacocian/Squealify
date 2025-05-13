namespace Squealify;
public static class Constants
{
    public const string Private = "private";
    public const string Protected = "protected";
    public const string Readonly = "readonly";
    public const string Public = "public";
    public const string Sealed = "sealed";
    public const string Partial = "partial";
    public const string Abstract = "abstract";
    public const string Async = "async";
    public const string Params = "params";

    public const string StringType = "string";
    public const string ByteType = "byte";
    public const string ValueTaskType = "ValueTask";
    public const string CancellationTokenType = "CancellationToken";
    public const string StringArrayType = "string[]";
    public const string DbConnectionType = "DbConnection";
    public const string DbParameterType = "DbParameter";
    public const string DbCommandType = "DbCommand";
    public const string FuncType = "Func";
    public const string NullableObject = "object?";
    public const string IAsyncEnumerableType = "IAsyncEnumerable";
    public const string EnumeratorCancellationAttribute = "EnumeratorCancellation";

    public const string PrimaryKey = "PRIMARY KEY";
    public const string ForeignKey = "FOREIGN KEY";
    public const string References = "REFERENCES";
    public const string NotNull = "NOT NULL";
    public const string Unique = "UNIQUE";

    public const string ArrayName = "Array";

    public const string Namespace = "Squealify";

    public const string UsingSystem = "System";
    public const string UsingSystemDataCommon = "System.Data.Common";
    public const string UsingSystemRuntimeCompilerServices = "System.Runtime.CompilerServices";
    public const string UsingSystemCollectionsGeneric = "System.Collections.Generic";
    public const string UsingSystemThreadingTasks = "System.Threading.Tasks";
    public const string UsingSystemThreading = "System.Threading";

    public const string CancellationTokenArgument = "cancellationToken";

    public const string DbSetName = "DbSet";
    public const string DbSetTypeName = "T";

    public const string TableAttributeName = "TableAttribute";
    public const string TableAttributeShortName = "Table";
    public const string TableNameProperty = "Name";
    public const string TableNameArgumentName = "name";

    public const string PrimaryKeyAttributeName = "PrimaryKeyAttribute";
    public const string PrimaryKeyAttributeShortName = "PrimaryKey";

    public const string ForeignKeyAttributeName = "ForeignKeyAttribute";
    public const string ForeignKeyAttributeShortName = "ForeignKey";
    public const string ForeignKeyReferenceTableProperty = "ReferenceTable";
    public const string ForeignKeyReferenceFieldProperty = "ReferenceField";
    public const string ForeignKeyArgumentReferenceTableName = "referenceTable";
    public const string ForeignKeyArgumentReferenceFieldName = "field";

    public const string ColumnNameAttributeName = "ColumnNameAttribute";
    public const string ColumnNameAttributeShortName = "ColumnName";
    public const string ColumnNameProperty = "Name";
    public const string ColumnNameArgumentName = "name";

    public const string VarcharAttributeName = "VarcharAttribute";
    public const string VarcharAttributeShortName = "Varchar";
    public const string LengthProperty = "Length";
    public const string LengthArgumentName = "length";

    public const string ColumnUniqueAttributeName = "ColumnUniqueAttribute";
    public const string ColumnUniqueAttributeShortName = "ColumnUnique";

    public const string TableUniqueAttributeName = "TableUniqueAttribute";
    public const string TableUniqueAttributeShortName = "TableUnique";
    public const string TableUniqueProperty = "Uniques";
    public const string TableUniqueArgumentName = "uniqueFields";

    public const string DbConnectionArgumentName = "connection";
    public const string DbConnectionPropertyName = "Connection";

    public const string CreateTableStatement = "CREATE TABLE";
    public const string CreateTableIfNotExistsStatement = "CREATE TABLE IF NOT EXISTS";
    public const string CreateTableMethod = "CreateTable";
    public const string CreateTableIfNotExistsMethod = "CreateTableIfNotExists";

    public const string CreateParameterMethod = "CreateParameter";
    public const string CreateParameterCommandArgument = "command";
    public const string CreateParameterNameArgument = "name";
    public const string CreateParameterValueArgument = "value";
    public const string ParameterTypeName = "TParameter";

    public const string DboArgumentName = "dbo";
    public const string PrimaryKeyArgumentName = "primaryKey";
    public const string InsertMethodName = "Insert";
    public const string UpsertMethodName = "Upsert";
    public const string UpdateMethodName = "Update";
    public const string DeleteMethodName = "Delete";
    public const string DeleteAllMethodName = "DeleteAll";
    public const string FindMethodName = "Find";
    public const string FindAllMethodName = "FindAll";
}
