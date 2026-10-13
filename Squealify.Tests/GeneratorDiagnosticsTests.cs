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
    [DataRow("SQF010", "namespace N; public partial class Q(System.Data.Common.DbConnection c) : SqlQueryContext(c) { [SqlQuery(\"SELECT 1\")] public static partial int A(); }")]
    [DataRow("SQF011", "public partial class Q(System.Data.Common.DbConnection c) : SqlQueryContext(c) { [SqlQuery(\"SELECT 1\")] public partial int A(); }")]
    [DataRow("SQF012", "namespace N; public partial class Q { [SqlQuery(\"SELECT 1\")] public partial int A(); }")]
    [DataRow("SQF013", "namespace N; public partial class Q(System.Data.Common.DbConnection c) : SqlQueryContext(c) { [SqlQuery(\"SELECT @a\")] public partial int A(object a); }")]
    [DataRow("SQF014", "namespace N; public partial class Q(System.Data.Common.DbConnection c) : SqlQueryContext(c) { [SqlQuery(\"SELECT @b\")] public partial int A(); }")]
    [DataRow("SQF015", "namespace N; public partial class Q(System.Data.Common.DbConnection c) : SqlQueryContext(c) { [SqlQuery(\"SELECT 1\")] public partial int A(int unused); }")]
    [DataRow("SQF016", "namespace N; public partial class Q(System.Data.Common.DbConnection c) : SqlQueryContext(c) { [SqlQuery(\"DELETE FROM t\")] public partial string A(); }")]
    [DataRow("SQF016", "namespace N; public partial class Q(System.Data.Common.DbConnection c) : SqlQueryContext(c) { [SqlQuery(\"SELECT 1\")] public partial System.IO.Stream A(); }")]
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

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StructTablesAndQueries_GenerateCompilableCode(bool synchronous)
    {
        var options = synchronous ? "[assembly: SquealifyOptions(Synchronous = true)]\n" : string.Empty;
        var (diagnostics, compilation) = RunGenerators(options + """
            namespace N;
            public enum Kind : byte { A, B }
            [Table("items", Naming = ColumnNaming.SnakeCase), TableIndex(nameof(Owner))]
            public struct Item { [PrimaryKey] public uint Id; public uint Owner { get; set; } public Kind Kind; public string? Name; }
            public sealed class Row { public uint Id { get; set; } public string? Name { get; set; } }
            public partial class ItemContext(System.Data.Common.DbConnection c) : ItemTableContextBase(c)
            {
                [SqlQuery("SELECT id, name FROM items WHERE owner = @owner")]
                public partial System.Collections.Generic.List<Row> Rows(uint owner);
                [SqlQuery("SELECT id, kind FROM items WHERE id = @id")]
                internal partial (uint Id, Kind Kind)? Pair(uint id);
                [SqlQuery("SELECT name FROM items WHERE id = @id")]
                public partial System.Threading.Tasks.ValueTask<string?> NameAsync(uint id, System.Threading.CancellationToken cancellationToken);
                [SqlQuery("UPDATE items SET name = @name WHERE id = @id")]
                public partial int Rename(uint id, string? name);
                [SqlQuery("DELETE FROM items WHERE id = @id")]
                private partial void Remove(uint id);
            }

            public partial class ItemContext
            {
                [SqlQuery("SELECT COUNT(*) FROM items")]
                public partial long Count();
            }
            """);

        Assert.IsEmpty(diagnostics);
        var compilationDiagnostics = compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToList();
        Assert.IsEmpty(compilationDiagnostics, string.Join(Environment.NewLine, compilationDiagnostics));
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
