using Microsoft.CodeAnalysis;
using Sybil;
using System;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class TableAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAttribute);
    }

    private static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var builder = SyntaxBuilder.CreateCompilationUnit()
            .WithNamespace(SyntaxBuilder.CreateNamespace(Constants.Namespace)
                .WithClass(SyntaxBuilder.CreateClass(Constants.TableAttributeName)
                    .WithBaseClass(nameof(Attribute))
                    .WithModifiers($"{Constants.Public} {Constants.Sealed}")
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.TableAttributeName)
                        .WithModifier(Constants.Public)
                        .WithBody($"this.{Constants.TableNameProperty} = string.Empty;"))
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.TableAttributeName)
                        .WithModifier(Constants.Public)
                        .WithParameter(Constants.StringType, Constants.TableNameArgumentName)
                        .WithBody($"this.{Constants.TableNameProperty} = {Constants.TableNameArgumentName};"))
                    .WithAttribute(SyntaxBuilder.CreateAttribute("AttributeUsage")
                        .WithArgument(AttributeTargets.Class)
                        .WithArgument("Inherited", false)
                        .WithArgument("AllowMultiple", false))
                    .WithProperty(SyntaxBuilder.CreateProperty(Constants.StringType, Constants.TableNameProperty)
                        .WithModifier(Constants.Public)
                        .WithAccessor(SyntaxBuilder.CreateGetter()))));

        var syntax = builder.Build();
        var source = syntax.ToFullString();
        context.AddSource($"{Constants.TableAttributeName}.g.cs", source);
    }
}
