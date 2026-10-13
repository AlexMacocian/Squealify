using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Squealify.Tests;

[TestClass]
public sealed class GeneratorDiagnosticsTests
{
    [TestMethod]
    [DataRow("SQF001", "[Table] public class T { public int Id { get; set; } }")]
    [DataRow("SQF002", "[Table] public class T { [PrimaryKey, AutoIncrement] public string Id { get; set; } = string.Empty; }")]
    [DataRow("SQF003", "[Table, TableIndex(\"Missing\")] public class T { [PrimaryKey] public int Id { get; set; } }")]
    [DataRow("SQF004", "[Table, TableForeignKey(\"r\", new[] { \"Id\" }, new string[0])] public class T { [PrimaryKey] public int Id { get; set; } }")]
    public void InvalidSchema_ReportsDiagnostic(string expectedId, string source)
    {
        var (diagnostics, _) = RunGenerators(source);

        Assert.Contains(expectedId, diagnostics.Select(d => d.Id));
    }

    [TestMethod]
    public void ValidSchema_GeneratesCompilableContext()
    {
        var (diagnostics, compilation) = RunGenerators(
            "[Table(\"t\")] [TableQuery(nameof(Value))] public class T { [PrimaryKey] public int Id { get; set; } public uint Value { get; set; } }");

        Assert.IsEmpty(diagnostics);
        var compilationDiagnostics = compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToList();
        Assert.IsEmpty(compilationDiagnostics, string.Join(Environment.NewLine, compilationDiagnostics));
        Assert.IsTrue(compilation.SyntaxTrees.Any(t => t.FilePath.EndsWith("TTableContextBase.g.cs", StringComparison.Ordinal)));
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, Compilation Compilation) RunGenerators(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => Path.GetFileNameWithoutExtension(path) != typeof(GeneratorDiagnosticsTests).Assembly.GetName().Name)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "Test",
            [CSharpSyntaxTree.ParseText($"#nullable enable\nusing Squealify;\n{source}", new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var generators = typeof(TableContextGenerator).Assembly.GetTypes()
            .Where(t => typeof(IIncrementalGenerator).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => ((IIncrementalGenerator)Activator.CreateInstance(t)!).AsSourceGenerator());

        CSharpGeneratorDriver.Create(generators, parseOptions: new CSharpParseOptions(LanguageVersion.Latest))
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        return (diagnostics, output);
    }
}
