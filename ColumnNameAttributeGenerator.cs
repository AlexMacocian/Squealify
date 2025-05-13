using Microsoft.CodeAnalysis;
using Sybil;
using System;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class ColumnNameAttributeGenerator : IIncrementalGenerator
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
                .WithClass(SyntaxBuilder.CreateClass(Constants.ColumnNameAttributeName)
                    .WithBaseClass(nameof(Attribute))
                    .WithModifiers($"{Constants.Public} {Constants.Sealed}")
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.ColumnNameAttributeName)
                        .WithModifier(Constants.Public)
                        .WithParameter(Constants.StringType, Constants.ColumnNameArgumentName)
                        .WithBody($"this.{Constants.ColumnNameProperty} = {Constants.ColumnNameArgumentName};"))
                    .WithAttribute(SyntaxBuilder.CreateAttribute("AttributeUsage")
                        .WithArgument(AttributeTargets.Property)
                        .WithArgument("Inherited", false)
                        .WithArgument("AllowMultiple", false))
                    .WithProperty(SyntaxBuilder.CreateProperty(Constants.StringType, Constants.ColumnNameProperty)
                        .WithModifier(Constants.Public)
                        .WithAccessor(SyntaxBuilder.CreateGetter()))));

        var syntax = builder.Build();
        var source = syntax.ToFullString();
        context.AddSource($"{Constants.ColumnNameAttributeName}.g", source);
    }
}
