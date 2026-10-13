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

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = new[]
    {
        MissingPrimaryKey,
        InvalidAutoIncrement,
        UnknownColumn,
        ForeignKeyColumnMismatch
    }.ToDictionary(d => d.Id);

    public static DiagnosticModel Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
        => new(descriptor.Id, arguments.ToEquatableArray(), LocationModel.FromLocation(location));

    public static Diagnostic ToDiagnostic(DiagnosticModel model)
        => Diagnostic.Create(
            Descriptors[model.Id],
            model.Location?.ToLocation(),
            model.Arguments.Cast<object>().ToArray());
}
