# Squealify

Compile-time micro-ORM for ADO.NET. Source generators turn schema classes and structs into table contexts, and `[SqlQuery]` partial methods into implementations, with SQL and row mapping generated at build time. No reflection, NativeAOT compatible.

The package contains the generators (`analyzers/`) and `Squealify.Abstractions` (`lib/`), which holds the attributes, enums and the `SqlQueryContext` base class shared by all consuming assemblies.

## Installation

`dotnet add package Squealify`

## Example

```csharp
[Table("characters")]
[TableIndex(nameof(AccountId), nameof(GameMode))]
[TableUnique(nameof(FirstName), nameof(LastName))]
public sealed class CharacterDbo
{
    [PrimaryKey]
    [AutoIncrement]
    public long Id { get; init; }

    [ForeignKey("accounts", "Id", OnDelete = ForeignKeyAction.Cascade)]
    public int AccountId { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int GameMode { get; set; }
}

public sealed class CharacterContext(DbConnection connection, SqlDialect dialect)
    : CharacterDboTableContextBase(connection, dialect);
```

```csharp
await using var characters = new CharacterContext(connection, SqlDialect.SQLite);
await characters.CreateTableIfNotExists(cancellationToken);
var id = await characters.Insert(new CharacterDbo { AccountId = 1, FirstName = "Arisen", LastName = "One" }, cancellationToken);
var character = await characters.FindByFirstNameAndLastName("Arisen", "One", cancellationToken);
await foreach (var c in characters.FindByAccountIdAndGameMode(1, 0, cancellationToken)) { }
```

## Attributes

| Attribute | Target | Effect |
|---|---|---|
| `[Table(name, Naming = ...)]` | class, struct | Table name (defaults to the type name); `ColumnNaming.SnakeCase` derives `character_id` from `CharacterId` |
| `[PrimaryKey]` | property, field | Primary key column; multiple members form a composite key |
| `[AutoIncrement]` | property, field | Database generated key; single `int`/`long` primary key only |
| `[ColumnName(name)]` | property, field | Column name (overrides `Naming`) |
| `[ColumnUnique]` | property, field | `UNIQUE` column; generates `FindBy{Column}` returning one row |
| `[Varchar(length)]` | property, field | `VARCHAR(length)` instead of `TEXT` |
| `[ForeignKey(table, column, OnDelete = ...)]` | property, field | Single column foreign key |
| `[TableForeignKey(table, columns, referenceColumns, OnDelete = ...)]` | class, struct | Composite foreign key |
| `[TableUnique(columns...)]` | class, struct | `UNIQUE` constraint; generates `FindBy{Columns}` returning one row |
| `[TableIndex(columns...)]` | class, struct | Index; generates `FindBy{Columns}` (all rows) and `DeleteBy{Columns}` |
| `[TableQuery(columns...)]` | class, struct | `FindBy{Columns}` and `DeleteBy{Columns}` without an index (e.g. primary key prefixes) |

| `[SqlQuery(sql, PostgreSql = ...)]` | partial method | Implements the method with the SQL (see [Queries](#queries)) |
| `[assembly: SquealifyOptions(Synchronous = true)]` | assembly | Synchronous table contexts (see [Synchronous mode](#synchronous-mode)) |
| `[assembly: SquealifyOptions(ContextBase = typeof(T))]` | assembly | Base class of generated table contexts; `T` derives from `SqlQueryContext` and has a `(DbConnection, SqlDialect)` constructor (e.g. to override `CreateParameter<T>` once for all tables) |

Columns are public instance properties with a getter and a setter, and public mutable instance fields. Columns in type level attributes accept member names (`nameof`) or column names.

## Generated members

| Member | Notes |
|---|---|
| `CreateTable`, `CreateTableIfNotExists` | DDL for the context's `SqlDialect`, including indexes |
| `Insert`, `InsertMany` | `Insert` returns the generated key for `[AutoIncrement]` tables |
| `InsertIfNotExists`, `InsertIfNotExistsMany` | `INSERT ... ON CONFLICT DO NOTHING`; return whether / how many rows were inserted |
| `Upsert`, `UpsertMany` | `INSERT ... ON CONFLICT (primary key) DO UPDATE` (`DO NOTHING` for key-only tables) |
| `Update`, `UpdateMany` | Returns affected rows; not generated for key-only tables |
| `Delete(keys...)`, `DeleteMany`, `DeleteAll` | Returns affected rows |
| `Find(keys...)`, `FindAll` | |
| `FindBy...`, `DeleteBy...` | From `[ColumnUnique]`, `[TableUnique]`, `[TableIndex]`, `[TableQuery]` |
| `ReadRow`, `TableName`, `SelectColumns` | For custom queries in the derived context |

Table contexts derive from `SqlQueryContext` (or the configured `ContextBase`):

| Member | Notes |
|---|---|
| `Transaction` | Assigned to every command the context executes |
| `Dialect` | `SqlDialect.SQLite` or `SqlDialect.PostgreSQL` |
| `CreateCommand`, `CreateCachedCommand` | Commands for custom code; cached commands are disposed with the context |
| `CreateParameter<T>`, `SetParameterValue<T>` | Virtual; override to use typed provider parameters |

Contexts implement `IDisposable` (and `IAsyncDisposable` in asynchronous mode), own their cached commands, and are not thread safe (one per connection).

## Queries

```csharp
public sealed partial class CharacterQueries(DbConnection connection, SqlDialect dialect) : SqlQueryContext(connection, dialect)
{
    [SqlQuery("""
        SELECT c."Id", c."FirstName", a."Name" AS account_name
        FROM "characters" c JOIN "accounts" a ON a."Id" = c."AccountId"
        WHERE c."AccountId" = @accountId
        """)]
    public partial List<CharacterSummary> FindSummaries(int accountId);

    [SqlQuery("""SELECT COUNT(*) FROM "characters" WHERE "AccountId" = @accountId""")]
    public partial ValueTask<long> CountAsync(int accountId, CancellationToken cancellationToken);

    [SqlQuery("""UPDATE "characters" SET "GameMode" = @gameMode WHERE "Id" = @id""")]
    public partial int SetGameMode(long id, int gameMode);
}
```

`[SqlQuery]` methods can also be declared in a class derived from a generated table context.

| Rule | Detail |
|---|---|
| Declaring class | Partial, non-nested, non-generic, in a namespace, derived from `SqlQueryContext` or a table context |
| Parameters | Scalars (see [Type mapping](#type-mapping), except converted types), bound to `@name`; `CancellationToken` is not bound. Every parameter must be used and every `@name` must be a parameter |
| Query or command | Statements starting with `SELECT`, `WITH`, `VALUES`, `PRAGMA`, `EXPLAIN`, `SHOW`, or containing `RETURNING`, are queries |
| Synchronous or asynchronous | As declared: `T` or `ValueTask<T>` |
| `void` | Executes the statement |
| `int` (command) | Rows affected |
| Scalar `T` | First column of the first row; throws when there is no row. `T?`: null when there is no row or the value is NULL |
| Tuple | Columns by position |
| Class or struct | Public settable members by column name; case and underscores are ignored (`account_name` matches `AccountName`); `[ColumnName]` overrides the name. Ordinals are resolved once per context. A member without a column throws |
| `List<T>` | All rows of any of the above |
| `PostgreSql` | Alternative SQL when the context dialect is PostgreSQL |
| SQL text | Must be a compile time constant that does not reference generated members (e.g. `SelectColumns`) |

One command per method is cached for the lifetime of the context.

## Synchronous mode

`[assembly: SquealifyOptions(Synchronous = true)]` generates synchronous table context methods instead of asynchronous ones:

| Asynchronous | Synchronous |
|---|---|
| `ValueTask<T> M(..., CancellationToken)` | `T M(...)` |
| `IReadOnlyList<T>` rows | `ReadOnlySpan<T>` rows |
| Struct rows by value | Struct rows by `in` reference |
| `IAsyncEnumerable<T> FindAll/FindBy...` | `List<T> FindAll/FindBy...(...)` and `void FindAll/FindBy...(..., List<T> results)` |

Combined with struct tables, rows can be kept in reusable arrays and written without allocations from Squealify (providers may still allocate).

## Performance

| Mechanism | Detail |
|---|---|
| Command caching | One prepared command per statement, parameters created once and reused |
| Multi-row statements | `*Many` methods send chunks of up to 64 rows per statement (`VALUES (...), (...)` / `IN (...)`), then 8, then 1 |
| No boxing (provider permitting) | Values flow through `SetParameterValue<T>`; override with e.g. `NpgsqlParameter<T>.TypedValue` |
| Typed reads | Rows are read with typed getters (`GetInt32`, `GetFieldValue<T>`) |

`UpsertMany` rows must have unique keys within one call (PostgreSQL rejects a statement that updates a row twice).

`Upsert` with explicit values on an `[AutoIncrement]` table does not advance the PostgreSQL identity sequence; later `Insert` calls can then collide.

## Type mapping

| CLR type | Parameter type | SQLite | PostgreSQL |
|---|---|---|---|
| `bool` | `bool` | `BOOLEAN` | `BOOLEAN` |
| `byte`, `sbyte`, `short` | `short` | `SMALLINT` | `SMALLINT` |
| `ushort`, `int` | `int` | `INTEGER` | `INTEGER` |
| `uint`, `long` | `long` | `BIGINT` | `BIGINT` |
| `ulong` | `long` (bit preserving) | `BIGINT` | `BIGINT` |
| `float` | `float` | `REAL` | `REAL` |
| `double` | `double` | `REAL` | `DOUBLE PRECISION` |
| `decimal` | `decimal` | `TEXT` | `NUMERIC` |
| `char`, `string` | `string` | `TEXT` / `VARCHAR(n)` | `TEXT` / `VARCHAR(n)` |
| `Guid` | `Guid` | `BLOB` | `UUID` |
| `DateTime` | `DateTime` (UTC expected) | `TEXT` | `TIMESTAMPTZ` |
| `DateTimeOffset` | `DateTimeOffset` (stored as UTC) | `TEXT` | `TIMESTAMPTZ` |
| `DateOnly`, `TimeOnly`, `TimeSpan` | same | `TEXT` | `DATE`, `TIME`, `INTERVAL` |
| `byte[]` | `byte[]` | `BLOB` | `BYTEA` |
| enums | per underlying type | per underlying type | per underlying type |
| other types | `string` via abstract `{Type}ToStringConverter` / `StringTo{Type}Converter` | `TEXT` | `TEXT` |

Nullable properties (`int?`, `string?`) map to nullable columns.

## Diagnostics

| Id | Meaning |
|---|---|
| SQF001 | Table has no `[PrimaryKey]` |
| SQF002 | `[AutoIncrement]` is not the single `int`/`long` primary key |
| SQF003 | Unknown column in a class level attribute |
| SQF004 | `[TableForeignKey]` column count mismatch |
| SQF010 | `[SqlQuery]` method is static, generic or not a partial method without implementation |
| SQF011 | `[SqlQuery]` class is nested, generic, not a class or in the global namespace |
| SQF012 | `[SqlQuery]` class does not derive from `SqlQueryContext` |
| SQF013 | Unsupported `[SqlQuery]` parameter type or modifier |
| SQF014 | SQL references an `@name` that is not a parameter |
| SQF015 | Parameter not referenced in the SQL |
| SQF016 | Unsupported `[SqlQuery]` return type |

## Notes

| Topic | Detail |
|---|---|
| SQLite foreign keys | Require `PRAGMA foreign_keys = ON;` per connection |
| Identifiers | All table and column names are quoted |
| Dialect | The `(DbConnection)` constructor uses `SqlDialect.SQLite`; pass the dialect explicitly for PostgreSQL |

## Tests

`dotnet test Squealify.slnx` runs `Squealify.Tests` (asynchronous mode) and `Squealify.Tests.Synchronous` (synchronous mode, struct tables) against in-memory SQLite. Set `SQUEALIFY_POSTGRESQL` to a connection string of a disposable database to also run the PostgreSQL suites.

## Upgrading from 0.9

| Change | Action |
|---|---|
| Table contexts derive from `SqlQueryContext`; `Dispose(bool)` replaces `Dispose()` overrides | Override `Dispose(bool disposing)` instead |
| `CreateCommand` is defined in `SqlQueryContext` | No action |

## Upgrading from 0.8

| Change | Action |
|---|---|
| Identifiers are quoted (`"Id"`) | PostgreSQL databases created by 0.8 used folded lower-case names; recreate or rename |
| `Guid`, `TimeSpan`, `byte`, `sbyte`, unsigned integers map natively | Remove their converter overrides; stored formats differ (e.g. `Guid` is `BLOB`/`UUID`) |
| Converter names start upper-case (`StringArrayToStringConverter`) | Rename overrides |
| Converters use the non-nullable type (`Func<T, string>` for `T?` properties) | Update override types |
| `Update`, `Delete`, `DeleteAll` return affected rows | No action for `await` callers |
| Attributes moved from generated sources to `Squealify.Abstractions` | No action |

