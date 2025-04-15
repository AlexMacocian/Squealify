using Microsoft.CodeAnalysis;
using Sybil;
using System;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class VarcharAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAttribute);
    }

    private static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = SyntaxBuilder.CreateCompilationUnit()
            .WithNamespace(SyntaxBuilder.CreateNamespace(Constants.Namespace)
                .WithClass(SyntaxBuilder.CreateClass(Constants.VarcharAttributeName)
                    .WithBaseClass(nameof(Attribute))
                    .WithModifiers($"{Constants.Public} {Constants.Sealed}")
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.VarcharAttributeName)
                        .WithModifier(Constants.Public)
                        .WithBody($"this.{Constants.LengthProperty} = 255;"))
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.VarcharAttributeName)
                        .WithModifier(Constants.Public)
                        .WithParameter(Constants.ByteType, Constants.LengthArgumentName)
                        .WithBody($"this.{Constants.LengthProperty} = {Constants.LengthArgumentName};"))
                    .WithAttribute(SyntaxBuilder.CreateAttribute("AttributeUsage")
                        .WithArgument(AttributeTargets.Property)
                        .WithArgument("Inherited", false)
                        .WithArgument("AllowMultiple", false))
                    .WithProperty(SyntaxBuilder.CreateProperty(Constants.ByteType, Constants.LengthProperty)
                        .WithModifier(Constants.Public)
                        .WithAccessor(SyntaxBuilder.CreateGetter()))));

        var syntax = builder.Build();
        var source = syntax.ToFullString();
        context.AddSource($"{Constants.VarcharAttributeName}.g.cs", source);
    }
}

