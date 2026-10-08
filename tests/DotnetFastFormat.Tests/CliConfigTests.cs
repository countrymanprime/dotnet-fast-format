using System.Text;
using DotnetFastFormat.Cli;
using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

public sealed class CliConfigTests : IDisposable
{
    private const string Unformatted = "namespace N\n{\n  class   A\n  {\n    int   x ;\n  }\n}\n";
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    private readonly string directory = Directory.CreateTempSubdirectory("fast-format-config-").FullName;

    public CliConfigTests() => Write(".editorconfig", "root = true\n");

    public void Dispose() => Directory.Delete(directory, recursive: true);

    [Fact]
    public void NestedConfigurationsApplyNearestFirst()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = tab\n");
        Write("sub/.editorconfig", "[*.cs]\nindent_style = space\nindent_size = 2\n");
        string top = Write("top.cs", Unformatted);
        string nested = Write("sub/inner.cs", Unformatted);
        string deeper = Write("sub/deeper/more.cs", Unformatted);

        var (code, _, error) = Run(directory);

        Assert.Equal(0, code);
        Assert.Equal(string.Empty, error);
        Assert.Equal("namespace N\n{\n\tclass A\n\t{\n\t\tint x;\n\t}\n}\n", File.ReadAllText(top));
        Assert.Equal("namespace N\n{\n  class A\n  {\n    int x;\n  }\n}\n", File.ReadAllText(nested));
        Assert.Equal(File.ReadAllText(nested), File.ReadAllText(deeper));
    }

    [Fact]
    public void RootTrueStopsTheSearchForFilesBelowIt()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_size = 8\n");
        Write("proj/.editorconfig", "root = true\n[*.cs]\nindent_size = 2\n");
        Write("proj/src/.editorconfig", "[*.cs]\nmax_line_length = 200\n");
        string inProject = Write("proj/src/a.cs", Unformatted);
        string outside = Write("b.cs", Unformatted);

        Assert.Equal(0, Run(directory).Code);

        Assert.Equal("namespace N\n{\n  class A\n  {\n    int x;\n  }\n}\n", File.ReadAllText(inProject));
        Assert.Equal("namespace N\n{\n        class A\n        {\n                int x;\n        }\n}\n", File.ReadAllText(outside));
    }

    [Fact]
    public void WithoutAConfigurationTheDefaultsApply()
    {
        string path = Write("a.cs", Unformatted);

        Assert.Equal(0, Run(path).Code);

        Assert.Equal("namespace N\n{\n    class A\n    {\n        int x;\n    }\n}\n", File.ReadAllText(path));
    }

    [Fact]
    public void ASectionOnlyAppliesToTheFilesItMatches()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_size = 2\n[Special.cs]\nindent_size = 3\n[gen/**.cs]\nindent_size = 1\n");
        string plain = Write("Plain.cs", Unformatted);
        string special = Write("sub/Special.cs", Unformatted);
        string generated = Write("gen/deep/G.cs", Unformatted);

        Assert.Equal(0, Run(directory).Code);

        Assert.StartsWith("namespace N\n{\n  class A", File.ReadAllText(plain), StringComparison.Ordinal);
        Assert.StartsWith("namespace N\n{\n   class A", File.ReadAllText(special), StringComparison.Ordinal);
        Assert.StartsWith("namespace N\n{\n class A", File.ReadAllText(generated), StringComparison.Ordinal);
    }

    [Fact]
    public void MaxLineLengthIsApplied()
    {
        Write(".editorconfig", "root = true\n[*]\nmax_line_length = 30\n");
        string path = Write("a.cs", "class A\n{\n    void Method(int first, int second) { }\n}\n");

        Assert.Equal(0, Run(path).Code);

        Assert.Equal("class A\n{\n    void Method(\n        int first,\n        int second\n    )\n    { }\n}\n", File.ReadAllText(path));
    }

    [Fact]
    public void TheBomIsKeptWhenCharsetIsNotSet()
    {
        string withBom = WriteBytes("with.cs", [.. Bom, .. Encoding.UTF8.GetBytes("class   A { }\n")]);
        string without = WriteBytes("without.cs", Encoding.UTF8.GetBytes("class   A { }\n"));

        Assert.Equal(0, Run(directory).Code);

        Assert.Equal([.. Bom, .. Encoding.UTF8.GetBytes("class A { }\n")], File.ReadAllBytes(withBom));
        Assert.Equal(Encoding.UTF8.GetBytes("class A { }\n"), File.ReadAllBytes(without));
    }

    [Fact]
    public void CharsetUtf8RemovesTheBom()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ncharset = utf-8\n");
        string path = WriteBytes("a.cs", [.. Bom, .. Encoding.UTF8.GetBytes("class A { }\n")]);

        var (code, output, _) = Run(path);

        Assert.Equal(0, code);
        Assert.Equal(Encoding.UTF8.GetBytes("class A { }\n"), File.ReadAllBytes(path));
        Assert.Contains("Formatted 1 file(s)", output, StringComparison.Ordinal);
    }

    [Fact]
    public void CharsetUtf8BomAddsTheBom()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ncharset = utf-8-bom\n");
        string path = WriteBytes("a.cs", Encoding.UTF8.GetBytes("class A { }\n"));
        string already = WriteBytes("b.cs", [.. Bom, .. Encoding.UTF8.GetBytes("class A { }\n")]);
        byte[] before = File.ReadAllBytes(already);

        var (code, output, _) = Run(directory);

        Assert.Equal(0, code);
        Assert.Equal([.. Bom, .. Encoding.UTF8.GetBytes("class A { }\n")], File.ReadAllBytes(path));
        Assert.Equal(before, File.ReadAllBytes(already));
        Assert.Contains("Formatted 1 file(s), 1 unchanged", output, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyFileDoesNotGetABomOrAnyText()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ncharset = utf-8-bom\ninsert_final_newline = true\n");
        string path = WriteBytes("empty.cs", []);

        Assert.Equal(0, Run(path).Code);

        Assert.Empty(File.ReadAllBytes(path));
    }

    [Fact]
    public void ABomOnlyFileStaysAsItIsWhenCharsetIsNotSet()
    {
        string path = WriteBytes("bom-only.cs", Bom);

        Assert.Equal(0, Run(path).Code);

        Assert.Equal(Bom, File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData("lf", "class A\n{\n    int x;\n}\n")]
    [InlineData("crlf", "class A\r\n{\r\n    int x;\r\n}\r\n")]
    public void EndOfLineConvertsTheFile(string value, string expected)
    {
        Write(".editorconfig", $"root = true\n[*.cs]\nend_of_line = {value}\n");
        string path = WriteBytes("a.cs", Encoding.UTF8.GetBytes("class   A\r\n{\r\n    int   x ;\n}\n"));

        Assert.Equal(0, Run(path).Code);

        Assert.Equal(expected, Encoding.UTF8.GetString(File.ReadAllBytes(path)));
    }

    [Fact]
    public void WithoutEndOfLineTheLineEndingsAreNotConverted()
    {
        string path = WriteBytes("a.cs", Encoding.UTF8.GetBytes("class   A\r\n{\r\n    int   x ;\r\n}\r\n"));

        Assert.Equal(0, Run(path).Code);

        Assert.Equal("class A\r\n{\r\n    int x;\r\n}\r\n", Encoding.UTF8.GetString(File.ReadAllBytes(path)));
    }

    [Fact]
    public void InsertFinalNewlineFalseRemovesTheFinalLineBreak()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ninsert_final_newline = false\n");
        string path = Write("a.cs", "class   A { }\n\n");

        Assert.Equal(0, Run(path).Code);

        Assert.Equal("class A { }", File.ReadAllText(path));
    }

    [Fact]
    public void ASecondRunChangesNothingUnderAnyOfTheseSettings()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = tab\nindent_size = 4\ntab_width = 3\nmax_line_length = 50\nend_of_line = crlf\ninsert_final_newline = false\ncharset = utf-8-bom\n");
        string path = Write("a.cs", Unformatted + "// note\n");

        Assert.Equal(0, Run(path).Code);
        byte[] first = File.ReadAllBytes(path);
        var (code, output, _) = Run(path);

        Assert.Equal(0, code);
        Assert.Equal(first, File.ReadAllBytes(path));
        Assert.Contains("0 file(s), 1 unchanged", output, StringComparison.Ordinal);
    }

    [Fact]
    public void AnInvalidValueWarnsAndKeepsTheExitCode()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_size = abc\nend_of_line = cr\n");
        string first = Write("a.cs", Unformatted);
        Write("b.cs", Unformatted);
        Write("sub/c.cs", Unformatted);

        var (code, output, error) = Run(directory);

        Assert.Equal(0, code);
        Assert.Equal("namespace N\n{\n    class A\n    {\n        int x;\n    }\n}\n", File.ReadAllText(first));
        string[] warnings = error.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, warnings.Length);
        Assert.All(warnings, warning => Assert.StartsWith("warning: ", warning, StringComparison.Ordinal));
        Assert.Contains(".editorconfig(3): indent_size = abc", warnings[0], StringComparison.Ordinal);
        Assert.Contains(".editorconfig(4): end_of_line = cr", warnings[1], StringComparison.Ordinal);
        Assert.DoesNotContain("warning", output, StringComparison.Ordinal);
        Assert.Contains("Formatted 3 file(s), 0 unchanged, 0 failed", output, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownKeysAreIgnoredWithoutAWarning()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ncsharp_new_line_before_open_brace = all\ndotnet_style_qualification_for_field = false:warning\ntrim_trailing_whitespace = true\n");
        string path = Write("a.cs", Unformatted);

        var (code, _, error) = Run(path);

        Assert.Equal(0, code);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void AnUnreadableEditorConfigDoesNotStopTheRun()
    {
        Write("proj/.editorconfig", "[*.cs]\nindent_size = 2\n");
        string path = Write("proj/a.cs", Unformatted);
        string unreadable = Path.Combine(directory, "proj", ".editorconfig");
        var output = new StringWriter();
        var error = new StringWriter();

        int code = CliApp.Run(
            [path],
            output,
            error,
            new RoslynFormatter(),
            new DiskFileStore(),
            config => string.Equals(config, unreadable, StringComparison.Ordinal) ? throw new IOException("denied") : File.Exists(config) ? File.ReadAllText(config) : null);

        Assert.Equal(0, code);
        Assert.Equal("namespace N\n{\n    class A\n    {\n        int x;\n    }\n}\n", File.ReadAllText(path));
        string warning = Assert.Single(error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("warning: ", warning, StringComparison.Ordinal);
        Assert.Contains("could not be read", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEditorConfigThatIsADirectoryIsIgnored()
    {
        Directory.CreateDirectory(Path.Combine(directory, "proj", ".editorconfig"));
        string path = Write("proj/a.cs", Unformatted);

        var (code, _, error) = Run(path);

        Assert.Equal(0, code);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void AnEditorConfigWithInvalidUtf8AndGarbageDoesNotCrash()
    {
        Directory.CreateDirectory(Path.Combine(directory, "proj"));
        File.WriteAllBytes(Path.Combine(directory, "proj", ".editorconfig"), [0xFF, 0xFE, (byte)'[', (byte)'*', (byte)']', (byte)'\n', 0x00, (byte)'x']);
        string path = Write("proj/a.cs", Unformatted);

        var (code, _, error) = Run(path);

        Assert.Equal(0, code);
        Assert.Contains("warning", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AFileThatFailsIsNotAffectedByItsConfiguration()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = tab\n");
        string broken = Write("broken.cs", "class C { void M( }\n");
        byte[] before = File.ReadAllBytes(broken);

        var (code, _, _) = Run(broken);

        Assert.Equal(2, code);
        Assert.Equal(before, File.ReadAllBytes(broken));
    }

    [Fact]
    public void HelpNamesTheSupportedKeys()
    {
        var output = new StringWriter();

        Assert.Equal(0, CliApp.Run(["--help"], output, new StringWriter()));

        string help = output.ToString();
        foreach (string key in new[] { "indent_style", "indent_size", "tab_width", "max_line_length", "end_of_line", "insert_final_newline", "charset" })
        {
            Assert.Contains(key, help, StringComparison.Ordinal);
        }
    }

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int code = CliApp.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    private string Write(string relative, string contents) => WriteBytes(relative, new UTF8Encoding(false).GetBytes(contents));

    private string WriteBytes(string relative, byte[] contents)
    {
        string path = Path.Combine(directory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, contents);
        return path;
    }
}
