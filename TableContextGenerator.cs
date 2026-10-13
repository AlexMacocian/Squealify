using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Squealify.Model;
using System.Linq;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class TableContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tables = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{Constants.Namespace}.{Constants.TableAttributeName}",
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (ctx, cancellationToken) => ModelBuilder.Build(ctx, cancellationToken));

        var options = context.ParseOptionsProvider.Select(static (parseOptions, _) => new GenerationOptions(
            FileScopedNamespace: parseOptions is CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp10 }));

        context.RegisterSourceOutput(tables.Combine(options), static (sourceProductionContext, pair) => Execute(sourceProductionContext, pair.Left, pair.Right));
    }

    private static void Execute(SourceProductionContext context, TableModel table, GenerationOptions options)
    {
        foreach (var diagnostic in table.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostics.ToDiagnostic(diagnostic));
        }

        if (table.Diagnostics.Any())
        {
            return;
        }

        context.AddSource(table.HintName, new TableContextEmitter(table, options).Emit());
    }
}
