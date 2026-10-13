# Squealify

Compile-time micro-ORM for ADO.NET. A source generator turns schema classes into table contexts with SQL and row mapping generated at build time. No reflection, NativeAOT compatible.

The package contains the generator (`analyzers/`) and `Squealify.Abstractions` (`lib/`), which holds the attributes and enums shared by all consuming assemblies.

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
| `[Table(name)]` | class | Table name (defaults to the class name) |
| `[PrimaryKey]` | property | Primary key column; multiple properties form a composite key |
| `[AutoIncrement]` | property | Database generated key; single `int`/`long` primary key only |
| `[ColumnName(name)]` | property | Column name (defaults to the property name) |
| `[ColumnUnique]` | property | `UNIQUE` column; generates `FindBy{Column}` returning one row |
| `[Varchar(length)]` | property | `VARCHAR(length)` instead of `TEXT` |
| `[ForeignKey(table, column, OnDelete = ...)]` | property | Single column foreign key |
| `[TableForeignKey(table, columns, referenceColumns, OnDelete = ...)]` | class | Composite foreign key |
| `[TableUnique(columns...)]` | class | `UNIQUE` constraint; generates `FindBy{Columns}` returning one row |
| `[TableIndex(columns...)]` | class | Index; generates `FindBy{Columns}` (all rows) and `DeleteBy{Columns}` |
| `[TableQuery(columns...)]` | class | `FindBy{Columns}` and `DeleteBy{Columns}` without an index (e.g. primary key prefixes) |

Columns in class level attributes accept property names (`nameof`) or column names.

## Generated members

| Member | Notes |
|---|---|
| `CreateTable`, `CreateTableIfNotExists` | DDL for the context's `SqlDialect`, including indexes |
| `Insert`, `InsertMany` | `Insert` returns the generated key for `[AutoIncrement]` tables |
| `Upsert`, `UpsertMany` | `INSERT ... ON CONFLICT (primary key) DO UPDATE` (`DO NOTHING` for key-only tables) |
| `Update`, `UpdateMany` | Returns affected rows; not generated for key-only tables |
| `Delete(keys...)`, `DeleteMany`, `DeleteAll` | Returns affected rows |
| `Find(keys...)`, `FindAll` | |
| `FindBy...`, `DeleteBy...` | From `[ColumnUnique]`, `[TableUnique]`, `[TableIndex]`, `[TableQuery]` |
| `ReadRow`, `CreateCommand`, `TableName`, `SelectColumns` | For custom queries in the derived context |
| `Transaction` | Assigned to every command the context executes |
| `CreateParameter<T>`, `SetParameterValue<T>` | Virtual; override to use typed provider parameters |

Contexts implement `IDisposable` and `IAsyncDisposable`, own their cached commands, and are not thread safe (one per connection).

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

## Notes

| Topic | Detail |
|---|---|
| SQLite foreign keys | Require `PRAGMA foreign_keys = ON;` per connection |
| Identifiers | All table and column names are quoted |
| Dialect | The `(DbConnection)` constructor uses `SqlDialect.SQLite`; pass the dialect explicitly for PostgreSQL |

## Tests

`dotnet test Squealify.Tests` runs against in-memory SQLite. Set `SQUEALIFY_POSTGRESQL` to a connection string of a disposable database to also run the PostgreSQL suite.

## Upgrading from 0.8

| Change | Action |
|---|---|
| Identifiers are quoted (`"Id"`) | PostgreSQL databases created by 0.8 used folded lower-case names; recreate or rename |
| `Guid`, `TimeSpan`, `byte`, `sbyte`, unsigned integers map natively | Remove their converter overrides; stored formats differ (e.g. `Guid` is `BLOB`/`UUID`) |
| Converter names start upper-case (`StringArrayToStringConverter`) | Rename overrides |
| Converters use the non-nullable type (`Func<T, string>` for `T?` properties) | Update override types |
| `Update`, `Delete`, `DeleteAll` return affected rows | No action for `await` callers |
| Attributes moved from generated sources to `Squealify.Abstractions` | No action |

