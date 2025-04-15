using Microsoft.CodeAnalysis;
using Sybil;
using System;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class ForeignKeyAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAttribute);
    }

    private static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = SyntaxBuilder.CreateCompilationUnit()
            .WithNamespace(SyntaxBuilder.CreateNamespace(Constants.Namespace)
                .WithClass(SyntaxBuilder.CreateClass(Constants.ForeignKeyAttributeName)
                    .WithBaseClass(nameof(Attribute))
                    .WithModifiers($"{Constants.Public} {Constants.Sealed}")
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.ForeignKeyAttributeName)
                        .WithModifier(Constants.Public)
                        .WithParameter(Constants.StringType, Constants.ForeignKeyArgumentReferenceTableName)
                        .WithParameter(Constants.StringType, Constants.ForeignKeyArgumentReferenceFieldName)
                        .WithBody($"this.{Constants.ForeignKeyReferenceTableProperty} = {Constants.ForeignKeyArgumentReferenceTableName}; this.{Constants.ForeignKeyReferenceFieldProperty} = {Constants.ForeignKeyArgumentReferenceFieldName};"))
                    .WithAttribute(SyntaxBuilder.CreateAttribute("AttributeUsage")
                        .WithArgument(AttributeTargets.Property)
                        .WithArgument("Inherited", false)
                        .WithArgument("AllowMultiple", false))
                    .WithProperty(SyntaxBuilder.CreateProperty(Constants.StringType, Constants.ForeignKeyReferenceTableProperty)
                        .WithModifier(Constants.Public)
                        .WithAccessor(SyntaxBuilder.CreateGetter()))
                    .WithProperty(SyntaxBuilder.CreateProperty(Constants.StringType, Constants.ForeignKeyReferenceFieldProperty)
                        .WithModifier(Constants.Public)
                        .WithAccessor(SyntaxBuilder.CreateGetter()))));
        var syntax = builder.Build();
        var source = syntax.ToFullString();
        context.AddSource($"{Constants.ForeignKeyAttributeName}.g.cs", source);
    }
}
