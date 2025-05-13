using Microsoft.CodeAnalysis;
using Sybil;
using System;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class PrimaryKeyAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAttribute);
    }

    private static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = SyntaxBuilder.CreateCompilationUnit()
            .WithNamespace(SyntaxBuilder.CreateNamespace(Constants.Namespace)
            .WithUsing(Constants.UsingSystem)
                .WithClass(SyntaxBuilder.CreateClass(Constants.PrimaryKeyAttributeName)
                    .WithBaseClass(nameof(Attribute))
                    .WithModifiers($"{Constants.Public} {Constants.Sealed}")
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.PrimaryKeyAttributeName)
                        .WithModifier(Constants.Public))
                    .WithAttribute(SyntaxBuilder.CreateAttribute("AttributeUsage")
                        .WithArgument(AttributeTargets.Property)
                        .WithArgument("Inherited", false)
                        .WithArgument("AllowMultiple", false))));
        var syntax = builder.Build();
        var source = syntax.ToFullString();
        context.AddSource($"{Constants.PrimaryKeyAttributeName}.g.cs", source);
    }
}
