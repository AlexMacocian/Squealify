using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Squealify.Model;
using System.Collections.Immutable;
using System.Linq;

namespace Squealify;

/// <summary>
/// Implements partial methods marked with [SqlQuery]; one generated file per declaring class.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SqlQueryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methods = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{Constants.Namespace}.{Constants.SqlQueryAttributeName}",
            predicate: static (node, _) => node is MethodDeclarationSyntax,
            transform: static (ctx, cancellationToken) => QueryModelBuilder.Build(ctx, cancellationToken));

        var options = TableContextGenerator.GetOptions(context);
        context.RegisterSourceOutput(methods.Collect().Combine(options), static (sourceProductionContext, pair) => Execute(sourceProductionContext, pair.Left, pair.Right));
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<QueryMethodModel> methods, GenerationOptions options)
    {
        foreach (var group in methods.GroupBy(m => (m.Namespace, m.ClassName)))
        {
            var valid = true;
            foreach (var method in group)
            {
                foreach (var diagnostic in method.Diagnostics)
                {
                    context.ReportDiagnostic(Diagnostics.ToDiagnostic(diagnostic));
                    valid = false;
                }
            }

            if (!valid)
            {
                continue;
            }

            var emitter = new QueryEmitter(group.Key.Namespace, group.Key.ClassName, group.ToList(), options);
            context.AddSource(emitter.HintName, emitter.Emit());
        }
    }
}
