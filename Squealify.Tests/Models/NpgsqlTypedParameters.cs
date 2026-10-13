using Npgsql;
using System.Data.Common;

namespace Squealify.Tests.Models;

/// <summary>
/// Typed Npgsql parameters: values are assigned through <see cref="NpgsqlParameter{T}.TypedValue"/> without boxing.
/// </summary>
public static class NpgsqlTypedParameters
{
    public static DbParameter? Create<TValue>(DbCommand command, string name)
        => command is NpgsqlCommand ? new NpgsqlParameter<TValue> { ParameterName = name } : null;

    public static bool TrySet<TValue>(DbParameter parameter, TValue value)
    {
        if (parameter is not NpgsqlParameter<TValue> typed)
        {
            return false;
        }

        typed.TypedValue = value;
        return true;
    }
}
