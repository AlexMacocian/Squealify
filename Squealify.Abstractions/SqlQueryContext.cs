using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Squealify;

/// <summary>
/// Base class of generated table contexts and of classes declaring <see cref="SqlQueryAttribute"/> methods.
/// Holds the connection, the current transaction, the commands cached for the lifetime of the context and the parameter hooks.
/// </summary>
/// <remarks>
/// Not thread safe: a context is bound to one connection.
/// </remarks>
public abstract class SqlQueryContext : IDisposable
{
    private readonly List<DbCommand> cachedCommands = [];

    protected SqlQueryContext(DbConnection connection)
        : this(connection, SqlDialect.SQLite)
    {
    }

    protected SqlQueryContext(DbConnection connection, SqlDialect dialect)
    {
        this.Connection = connection;
        this.Dialect = dialect;
    }

    public SqlDialect Dialect { get; }

    /// <summary>
    /// Transaction assigned to every command executed by the context.
    /// </summary>
    public DbTransaction? Transaction { get; set; }

    protected DbConnection Connection { get; }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a command owned by the caller.
    /// </summary>
    protected DbCommand CreateCommand(string commandText)
    {
        var command = this.Connection.CreateCommand();
        command.CommandText = commandText;
        command.Transaction = this.Transaction;
        return command;
    }

    /// <summary>
    /// Creates a command that is reused for the lifetime of the context and disposed with it.
    /// </summary>
    protected DbCommand CreateCachedCommand(string commandText)
    {
        var command = this.CreateCommand(commandText);
        this.cachedCommands.Add(command);
        return command;
    }

    /// <summary>
    /// Creates a parameter whose values will be of type <typeparamref name="TValue"/>. Override to create provider typed parameters.
    /// </summary>
    protected virtual DbParameter CreateParameter<TValue>(DbCommand command, string name)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        return parameter;
    }

    /// <summary>
    /// Sets a parameter value. Override to set provider typed parameters without boxing.
    /// </summary>
    protected virtual void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        parameter.Value = value is null ? DBNull.Value : (object)value;
    }

    protected DbParameter CreateParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }

    /// <summary>
    /// Maps result columns to members. <paramref name="members"/> are normalized member names (see <see cref="NormalizeName"/>);
    /// the result holds, per member, the ordinal of the first column with the same normalized name.
    /// </summary>
    /// <exception cref="InvalidOperationException">A member has no column in the result.</exception>
    protected static int[] ResolveOrdinals(DbDataReader reader, string[] members)
    {
        var ordinals = new int[members.Length];
        for (var member = 0; member < members.Length; member++)
        {
            ordinals[member] = -1;
        }

        for (var ordinal = reader.FieldCount - 1; ordinal >= 0; ordinal--)
        {
            var name = NormalizeName(reader.GetName(ordinal));
            for (var member = 0; member < members.Length; member++)
            {
                if (string.Equals(members[member], name, StringComparison.Ordinal))
                {
                    ordinals[member] = ordinal;
                }
            }
        }

        for (var member = 0; member < members.Length; member++)
        {
            if (ordinals[member] < 0)
            {
                throw new InvalidOperationException($"The query result has no column for member '{members[member]}'.");
            }
        }

        return ordinals;
    }

    /// <summary>
    /// DateTime values are stored and read as UTC: local times are converted, unspecified times are taken as UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };

    /// <summary>
    /// Lower case, without underscores: <c>character_id</c>, <c>CharacterId</c> and <c>characterId</c> are equal.
    /// </summary>
    public static string NormalizeName(string name)
    {
        var buffer = new char[name.Length];
        var length = 0;
        foreach (var c in name)
        {
            if (c != '_')
            {
                buffer[length++] = char.ToLowerInvariant(c);
            }
        }

        return new string(buffer, 0, length);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        foreach (var command in this.cachedCommands)
        {
            command.Dispose();
        }

        this.cachedCommands.Clear();
    }
}
