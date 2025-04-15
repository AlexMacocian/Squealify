# Squealify

Squealify is a lightweight, compile-time micro-ORM for .NET that generates database query methods based on schema classes. It leverages C# source generators to create strongly-typed, efficient, and maintainable database access code.

## Features

- **Compile-Time Query Generation**: Automatically generates database query methods (e.g., `Create`, `Insert`, `Upsert`, `Update`, `Delete`, `Find`) based on your schema class.
- **Schema-Driven**: Define your database schema using attributes like `[Table]`, `[PrimaryKey]`, and `[ColumnUnique]`.
- **Nullable support**: Supports nullable properties and types, allowing for flexible schema definitions. Define properties as nullable in your schema and Squealify will generate nullable columns.
- **Type Safety**: Strongly-typed query methods ensure compile-time safety.
- **Custom Conversions**: Supports custom type conversions for complex types. Automatically detects complex types and forces you to implement the conversion on compilation.
- **Minimal Boilerplate**: Focus on your schema, and let Squealify handle the rest.

## Installation

Add Squealify to your project via NuGet:

`dotnet add package Squealify`

## Getting Started

### Step 1: Define Your Schema Class

Define a class that represents your database table. Use attributes to specify table and column metadata.

```csharp
[Table("accounts")]
[TableUnique(nameof(Name), nameof(Id))]
public sealed class AccountDBO
{
    [PrimaryKey]
    public required AccountIdentifier Id { get; init; }

    [ForeignKey("secondaryTable", "id")]
    public required Identifier Secondary { get; init; }
    
    [ColumnUnique]
    public required string Name { get; init; }

    [ColumnName("password")]
    public string? PasswordHash { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset? EmailVerifiedTime { get; set; }

    public AccountType? State { get; set; }

    public DateTimeOffset? CreatedTime { get; set; }
    public DateTimeOffset? LastLoginTime { get; set; }
}
```

### Step 2: Squealify generates your db context
```csharp
public abstract class AccountDBOTableContextBase
{
    public AccountDBOTableContextBase(DbConnection connection)
    {
        this.Connection = connection;
    }

    protected DbConnection Connection { get; }
    protected abstract Func<AccountIdentifier, string> AccountIdentifierToStringConverter { get; }
    protected abstract Func<string, AccountIdentifier> StringToAccountIdentifierConverter { get; }
    protected abstract Func<Identifier, string> IdentifierToStringConverter { get; }
    protected abstract Func<string, Identifier> StringToIdentifierConverter { get; }

    public async ValueTask CreateTable(CancellationToken cancellationToken)
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = @"
			CREATE TABLE accounts (
				Id TEXT PRIMARY KEY,
				Secondary TEXT NOT NULL REFERENCES secondaryTable(id),
				Name TEXT UNIQUE NOT NULL,
				password TEXT,
				Email TEXT,
				EmailVerifiedTime TIMESTAMP,
				State INTEGER,
				CreatedTime TIMESTAMP,
				LastLoginTime TIMESTAMP,
				UNIQUE (Name, Id));
";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask CreateTableIfNotExists(CancellationToken cancellationToken)
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = @"
			CREATE TABLE IF NOT EXISTS accounts (
				Id TEXT PRIMARY KEY,
				Secondary TEXT NOT NULL REFERENCES secondaryTable(id),
				Name TEXT UNIQUE NOT NULL,
				password TEXT,
				Email TEXT,
				EmailVerifiedTime TIMESTAMP,
				State INTEGER,
				CreatedTime TIMESTAMP,
				LastLoginTime TIMESTAMP,
				UNIQUE (Name, Id));
";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask Insert(AccountDBO dbo, CancellationToken cancellationToken)
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = @"
			INSERT INTO accounts
				(Id, Secondary, Name, password, Email, EmailVerifiedTime, State, CreatedTime, LastLoginTime)
			VALUES
				(@Id, @Secondary, @Name, @password, @Email, @EmailVerifiedTime, @State, @CreatedTime, @LastLoginTime)
";
        command.Parameters.Add(this.CreateParameter(command, "@Id", this.AccountIdentifierToStringConverter(dbo.Id)));
        command.Parameters.Add(this.CreateParameter(command, "@Secondary", this.IdentifierToStringConverter(dbo.Secondary)));
        command.Parameters.Add(this.CreateParameter(command, "@Name", dbo.Name));
        command.Parameters.Add(this.CreateParameter(command, "@password", dbo.PasswordHash));
        command.Parameters.Add(this.CreateParameter(command, "@Email", dbo.Email));
        command.Parameters.Add(this.CreateParameter(command, "@EmailVerifiedTime", dbo.EmailVerifiedTime));
        command.Parameters.Add(this.CreateParameter(command, "@State", dbo.State));
        command.Parameters.Add(this.CreateParameter(command, "@CreatedTime", dbo.CreatedTime));
        command.Parameters.Add(this.CreateParameter(command, "@LastLoginTime", dbo.LastLoginTime));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask Upsert(AccountDBO dbo, CancellationToken cancellationToken)
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = @"
			INSERT INTO accounts
				(Id, Secondary, Name, password, Email, EmailVerifiedTime, State, CreatedTime, LastLoginTime)
			VALUES
				(@Id, @Secondary, @Name, @password, @Email, @EmailVerifiedTime, @State, @CreatedTime, @LastLoginTime)
			ON CONFLICT(Id) DO UPDATE SET
				Secondary = excluded.Secondary,
				Name = excluded.Name,
				password = excluded.password,
				Email = excluded.Email,
				EmailVerifiedTime = excluded.EmailVerifiedTime,
				State = excluded.State,
				CreatedTime = excluded.CreatedTime,
				LastLoginTime = excluded.LastLoginTime;
";
        command.Parameters.Add(this.CreateParameter(command, "@Id", this.AccountIdentifierToStringConverter(dbo.Id)));
        command.Parameters.Add(this.CreateParameter(command, "@Secondary", this.IdentifierToStringConverter(dbo.Secondary)));
        command.Parameters.Add(this.CreateParameter(command, "@Name", dbo.Name));
        command.Parameters.Add(this.CreateParameter(command, "@password", dbo.PasswordHash));
        command.Parameters.Add(this.CreateParameter(command, "@Email", dbo.Email));
        command.Parameters.Add(this.CreateParameter(command, "@EmailVerifiedTime", dbo.EmailVerifiedTime));
        command.Parameters.Add(this.CreateParameter(command, "@State", dbo.State));
        command.Parameters.Add(this.CreateParameter(command, "@CreatedTime", dbo.CreatedTime));
        command.Parameters.Add(this.CreateParameter(command, "@LastLoginTime", dbo.LastLoginTime));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask Update(AccountDBO dbo, CancellationToken cancellationToken)
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = @"
			UPDATE accounts
			SET
				Secondary = @Secondary,
				Name = @Name,
				password = @password,
				Email = @Email,
				EmailVerifiedTime = @EmailVerifiedTime,
				State = @State,
				CreatedTime = @CreatedTime,
				LastLoginTime = @LastLoginTime
			WHERE
				Id = @Id;
";
        command.Parameters.Add(this.CreateParameter(command, "@Id", this.AccountIdentifierToStringConverter(dbo.Id)));
        command.Parameters.Add(this.CreateParameter(command, "@Secondary", this.IdentifierToStringConverter(dbo.Secondary)));
        command.Parameters.Add(this.CreateParameter(command, "@Name", dbo.Name));
        command.Parameters.Add(this.CreateParameter(command, "@password", dbo.PasswordHash));
        command.Parameters.Add(this.CreateParameter(command, "@Email", dbo.Email));
        command.Parameters.Add(this.CreateParameter(command, "@EmailVerifiedTime", dbo.EmailVerifiedTime));
        command.Parameters.Add(this.CreateParameter(command, "@State", dbo.State));
        command.Parameters.Add(this.CreateParameter(command, "@CreatedTime", dbo.CreatedTime));
        command.Parameters.Add(this.CreateParameter(command, "@LastLoginTime", dbo.LastLoginTime));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask Delete(AccountIdentifier primaryKey, CancellationToken cancellationToken)
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = @"
			DELETE FROM accounts
			WHERE Id = @Id;
";
        command.Parameters.Add(this.CreateParameter(command, "@Id", this.AccountIdentifierToStringConverter(primaryKey)));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask<AccountDBO> Find(AccountIdentifier primaryKey, CancellationToken cancellationToken)
    {
        using var command = this.Connection.CreateCommand();
        command.CommandText = @"
			SELECT * FROM accounts
			WHERE Id = @Id;
";
        command.Parameters.Add(this.CreateParameter(command, "@Id", this.AccountIdentifierToStringConverter(primaryKey)));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return default;
        return new AccountDBO
        {
        Id = this.StringToAccountIdentifierConverter(reader.GetString(0)) , 
        Secondary = this.StringToIdentifierConverter(reader.GetString(1)) , 
        Name = reader.GetString(2) , 
        PasswordHash = await reader.IsDBNullAsync(3, cancellationToken) ? default : reader.GetString(3) , 
        Email = await reader.IsDBNullAsync(4, cancellationToken) ? default : reader.GetString(4) , 
        EmailVerifiedTime = await reader.IsDBNullAsync(5, cancellationToken) ? default : (DateTimeOffset? )reader.GetDateTime(5) , 
        State = await reader.IsDBNullAsync(6, cancellationToken) ? default : (AccountType? )reader.GetInt32(6) , 
        CreatedTime = await reader.IsDBNullAsync(7, cancellationToken) ? default : (DateTimeOffset? )reader.GetDateTime(7) , 
        LastLoginTime = await reader.IsDBNullAsync(8, cancellationToken) ? default : (DateTimeOffset? )reader.GetDateTime(8) } ; 
    }

    protected DbParameter CreateParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }
}
```


### Step 3: Implement the Context

Create an implementation of the generated context and provide any required custom type converters.
Squealify generates basic queries and statements based on your schema.

```csharp
public sealed class AccountsContext(DbConnection connection) : AccountDBOTableContextBase(connection)
{
    protected override Func<AccountIdentifier, string> AccountIdentifierToStringConverter { get; } = (identifier) => identifier.ToString();
    protected override Func<string, AccountIdentifier> StringToAccountIdentifierConverter { get; } = Identifier.ParseIdentifier<AccountIdentifier>;
    protected override Func<Identifier, string> IdentifierToStringConverter { get; } = (identifier) => identifier.ToString();
    protected override Func<string, Identifier> StringToIdentifierConverter { get; } = Identifier.ParseIdentifier<AccountIdentifier>;
}
```


### Step 4: Use the Context

Use the context in your application to interact with the database.

```csharp
var accountsContext = scope.ServiceProvider.GetRequiredService<AccountsContext>();
await accountsContext.CreateTableIfNotExists(cancellationToken);
await accountsContext.Upsert(new Models.AccountDBO { Id = Identifier.Create<AccountIdentifier>(), Secondary = Identifier.Create<AccountIdentifier>(), Name = "Test", PasswordHash = "1234" }, cancellationToken);
await accountsContext.Find(Identifier.ParseIdentifier<AccountIdentifier>("1234"), cancellationToken);
```

## Attributes
Squealify provides the following attributes to define your schema:

- `[Table(string name)]`: Specifies the table name.
- `[PrimaryKey]`: Marks a property as the primary key.
- `[ForeignKey(string referenceTable, string referenceField)]`: Defines a foreign key relationship.
- `[ColumnUnique]`: Marks a property as unique.
- `[TableUnique]`: Defines a table unique that can be a combination of any columns.
- `[Varchar(int length)]`: Specifies a `VARCHAR` column with a maximum length.

## Supported Queries

The following methods are generated for each schema:

- `CreateTable`
- `CreateTableIfNotExists`
- `Insert`
- `Upsert`
- `Update`
- `Delete`
- `Find`