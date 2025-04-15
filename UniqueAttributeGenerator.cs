using Microsoft.CodeAnalysis;
using Sybil;
using System;

namespace Squealify;

[Generator(LanguageNames.CSharp)]
public sealed class UniqueAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAttribute);
    }

    private static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var columnUniqueBuilder = SyntaxBuilder.CreateCompilationUnit()
            .WithNamespace(SyntaxBuilder.CreateNamespace(Constants.Namespace)
                .WithClass(SyntaxBuilder.CreateClass(Constants.ColumnUniqueAttributeName)
                    .WithBaseClass(nameof(Attribute))
                    .WithModifiers($"{Constants.Public} {Constants.Sealed}")
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.ColumnUniqueAttributeName)
                        .WithModifier(Constants.Public))
                    .WithAttribute(SyntaxBuilder.CreateAttribute("AttributeUsage")
                        .WithArgument(AttributeTargets.Property)
                        .WithArgument("Inherited", false)
                        .WithArgument("AllowMultiple", false))));
        var columnUniqueSource = columnUniqueBuilder.Build().ToFullString();
        context.AddSource($"{Constants.ColumnUniqueAttributeName}.g.cs", columnUniqueSource);

        var tableUniqueBuilder = SyntaxBuilder.CreateCompilationUnit()
            .WithNamespace(SyntaxBuilder.CreateNamespace(Constants.Namespace)
                .WithClass(SyntaxBuilder.CreateClass(Constants.TableUniqueAttributeName)
                    .WithBaseClass(nameof(Attribute))
                    .WithModifiers($"{Constants.Public} {Constants.Sealed}")
                    .WithConstructor(SyntaxBuilder.CreateConstructor(Constants.TableUniqueAttributeName)
                        .WithModifier(Constants.Public)
                        .WithParameter($"{Constants.Params} {Constants.StringArrayType}", Constants.TableUniqueArgumentName)
                        .WithBody($"this.{Constants.TableUniqueProperty} = {Constants.TableUniqueArgumentName};"))
                    .WithAttribute(SyntaxBuilder.CreateAttribute("AttributeUsage")
                        .WithArgument(AttributeTargets.Class)
                        .WithArgument("Inherited", false)
                        .WithArgument("AllowMultiple", true))
                    .WithProperty(SyntaxBuilder.CreateProperty(Constants.StringArrayType, Constants.TableUniqueProperty)
                        .WithModifiers(Constants.Public)
                        .WithAccessor(SyntaxBuilder.CreateGetter()))));
        var tableUniqueSource = tableUniqueBuilder.Build().ToFullString();
        context.AddSource($"{Constants.TableUniqueAttributeName}.g.cs", tableUniqueSource);
    }
}
