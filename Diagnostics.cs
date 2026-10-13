using Microsoft.CodeAnalysis;
using Squealify.Model;
using System.Collections.Generic;
using System.Linq;

namespace Squealify;

internal static class Diagnostics
{
    private const string Category = "Squealify";

    public static readonly DiagnosticDescriptor MissingPrimaryKey = new(
        "SQF001",
        "Table has no primary key",
        "Table '{0}' must have at least one property marked with [PrimaryKey]",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor InvalidAutoIncrement = new(
        "SQF002",
        "Invalid [AutoIncrement]",
        "[AutoIncrement] on '{0}' requires it to be the only primary key and of type int or long",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnknownColumn = new(
        "SQF003",
        "Unknown column",
        "Column '{0}' referenced by [{1}] does not exist on '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor ForeignKeyColumnMismatch = new(
        "SQF004",
        "Foreign key column count mismatch",
        "[TableForeignKey] on '{0}' must declare the same number of columns and reference columns",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor InvalidQueryMethod = new(
        "SQF010",
        "Invalid [SqlQuery] method",
        "[SqlQuery] method '{0}' must be a non-static, non-generic partial method without an implementation",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor InvalidQueryContainer = new(
        "SQF011",
        "Invalid [SqlQuery] container",
        "[SqlQuery] method '{0}' must be declared in a non-nested, non-generic partial class inside a namespace",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MissingQueryContext = new(
        "SQF012",
        "[SqlQuery] class does not derive from SqlQueryContext",
        "Class '{0}' declaring [SqlQuery] methods must derive from SqlQueryContext or a generated table context",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnsupportedQueryParameter = new(
        "SQF013",
        "Unsupported [SqlQuery] parameter",
        "Parameter '{0}' of '{1}' must be a by-value scalar (primitive, enum, string, byte[], Guid, date or time type) or a CancellationToken",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnknownSqlParameter = new(
        "SQF014",
        "Unknown SQL parameter",
        "The SQL of '{0}' references '@{1}', which is not a parameter of the method",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnusedQueryParameter = new(
        "SQF015",
        "Unused [SqlQuery] parameter",
        "Parameter '{0}' of '{1}' is not referenced as '@{0}' in its SQL",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor UnsupportedQueryReturn = new(
        "SQF016",
        "Unsupported [SqlQuery] return type",
        "Return type of '{0}' is not supported: {1}",
        Category,
        DiagnosticSeverity.Error,
        true);

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = new[]
    {
        MissingPrimaryKey,
        InvalidAutoIncrement,
        UnknownColumn,
        ForeignKeyColumnMismatch,
        InvalidQueryMethod,
        InvalidQueryContainer,
        MissingQueryContext,
        UnsupportedQueryParameter,
        UnknownSqlParameter,
        UnusedQueryParameter,
        UnsupportedQueryReturn
    }.ToDictionary(d => d.Id);

    public static DiagnosticModel Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
        => new(descriptor.Id, arguments.ToEquatableArray(), LocationModel.FromLocation(location));

    public static Diagnostic ToDiagnostic(DiagnosticModel model)
        => Diagnostic.Create(
            Descriptors[model.Id],
            model.Location?.ToLocation(),
            model.Arguments.Cast<object>().ToArray());
}
