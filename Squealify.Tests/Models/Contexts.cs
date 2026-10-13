using System.Data.Common;

namespace Squealify.Tests.Models;

public sealed class AllTypesContext(DbConnection connection, SqlDialect dialect) : AllTypesDboTableContextBase(connection, dialect)
{
    protected override Func<CustomId, string> CustomIdToStringConverter { get; } = id => id.Value;
    protected override Func<string, CustomId> StringToCustomIdConverter { get; } = value => new CustomId(value);

    protected override DbParameter CreateParameter<TValue>(DbCommand command, string name)
        => NpgsqlTypedParameters.Create<TValue>(command, name) ?? base.CreateParameter<TValue>(command, name);

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        if (!NpgsqlTypedParameters.TrySet(parameter, value))
        {
            base.SetParameterValue(parameter, value);
        }
    }
}

public sealed class AccountContext(DbConnection connection, SqlDialect dialect) : AccountDboTableContextBase(connection, dialect)
{
    protected override DbParameter CreateParameter<TValue>(DbCommand command, string name)
        => NpgsqlTypedParameters.Create<TValue>(command, name) ?? base.CreateParameter<TValue>(command, name);

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        if (!NpgsqlTypedParameters.TrySet(parameter, value))
        {
            base.SetParameterValue(parameter, value);
        }
    }
}

public sealed partial class CharacterContext(DbConnection connection, SqlDialect dialect) : CharacterDboTableContextBase(connection, dialect)
{
    [SqlQuery(@"SELECT ""Id"", ""AccountId"", ""FirstName"", ""LastName"", ""GameMode"" FROM ""characters"" WHERE ""GameMode"" = @gameMode ORDER BY ""Id""")]
    public partial List<CharacterDbo> FindByGameModeOrdered(int gameMode);

    protected override DbParameter CreateParameter<TValue>(DbCommand command, string name)
        => NpgsqlTypedParameters.Create<TValue>(command, name) ?? base.CreateParameter<TValue>(command, name);

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        if (!NpgsqlTypedParameters.TrySet(parameter, value))
        {
            base.SetParameterValue(parameter, value);
        }
    }
}

public sealed class StorageItemContext(DbConnection connection, SqlDialect dialect) : StorageItemDboTableContextBase(connection, dialect)
{
    public List<Type> ParameterTypes { get; } = [];

    protected override DbParameter CreateParameter<TValue>(DbCommand command, string name)
        => NpgsqlTypedParameters.Create<TValue>(command, name) ?? base.CreateParameter<TValue>(command, name);

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        this.ParameterTypes.Add(typeof(TValue));
        if (!NpgsqlTypedParameters.TrySet(parameter, value))
        {
            base.SetParameterValue(parameter, value);
        }
    }
}

public sealed class CrestContext(DbConnection connection, SqlDialect dialect) : CrestDboTableContextBase(connection, dialect)
{
    protected override DbParameter CreateParameter<TValue>(DbCommand command, string name)
        => NpgsqlTypedParameters.Create<TValue>(command, name) ?? base.CreateParameter<TValue>(command, name);

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        if (!NpgsqlTypedParameters.TrySet(parameter, value))
        {
            base.SetParameterValue(parameter, value);
        }
    }
}

public sealed class UnlockContext(DbConnection connection, SqlDialect dialect) : UnlockDboTableContextBase(connection, dialect)
{
    protected override DbParameter CreateParameter<TValue>(DbCommand command, string name)
        => NpgsqlTypedParameters.Create<TValue>(command, name) ?? base.CreateParameter<TValue>(command, name);

    protected override void SetParameterValue<TValue>(DbParameter parameter, TValue value)
    {
        if (!NpgsqlTypedParameters.TrySet(parameter, value))
        {
            base.SetParameterValue(parameter, value);
        }
    }
}

public sealed class SequenceContext(DbConnection connection, SqlDialect dialect) : SequenceDboTableContextBase(connection, dialect);
