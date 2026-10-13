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
            predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            transform: static (ctx, cancellationToken) => ModelBuilder.Build(ctx, cancellationToken));

        var options = GetOptions(context);

        context.RegisterSourceOutput(tables.Combine(options), static (sourceProductionContext, pair) => Execute(sourceProductionContext, pair.Left, pair.Right));
    }

    /// <summary>
    /// Language version and assembly level [SquealifyOptions]. Shared with the query generator.
    /// </summary>
    internal static IncrementalValueProvider<GenerationOptions> GetOptions(IncrementalGeneratorInitializationContext context)
    {
        var fileScoped = context.ParseOptionsProvider.Select(static (parseOptions, _) =>
            parseOptions is CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp10 });
        var assemblyOptions = context.CompilationProvider.Select(static (compilation, _) => ReadAssemblyOptions(compilation));
        return fileScoped.Combine(assemblyOptions).Select(static (pair, _) => new GenerationOptions(pair.Left, pair.Right.Synchronous, pair.Right.ContextBase));
    }

    private static (bool Synchronous, string? ContextBase) ReadAssemblyOptions(Compilation compilation)
    {
        var synchronous = false;
        string? contextBase = null;
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != $"{Constants.Namespace}.{Constants.OptionsAttributeName}")
            {
                continue;
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == Constants.OptionsSynchronousProperty && argument.Value.Value is bool value)
                {
                    synchronous = value;
                }
                else if (argument.Key == Constants.OptionsContextBaseProperty && argument.Value.Value is ITypeSymbol type)
                {
                    contextBase = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }

        return (synchronous, contextBase);
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
