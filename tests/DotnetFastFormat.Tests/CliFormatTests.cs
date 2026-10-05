using System.Text;
using DotnetFastFormat.Cli;
using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

public sealed class CliFormatTests : IDisposable
{
    private const string Unformatted = "class   A { }\n";
    private const string Formatted = "class A { }\n";

    private readonly string directory = Directory.CreateTempSubdirectory("fast-format-cli-").FullName;

    public void Dispose() => Directory.Delete(directory, recursive: true);

    [Fact]
    public void DiscoversCSharpFiles()
    {
        string a = Write("a.cs", Unformatted);
        string b = Write("sub/deeper/b.cs", Unformatted);
        string other = Write("notes.txt", Unformatted);

        var (code, output, _) = Run(directory);

        Assert.Equal(0, code);
        Assert.Equal(Formatted, File.ReadAllText(a));
        Assert.Equal(Formatted, File.ReadAllText(b));
        Assert.Equal(Unformatted, File.ReadAllText(other));
        Assert.Contains("Formatted 2 file(s)", output, StringComparison.Ordinal);
    }

    [Fact]
    public void NamedFilesAreFormattedWhateverTheirExtension()
    {
        string path = Write("snippet.txt", Unformatted);

        Assert.Equal(0, Run(path).Code);
        Assert.Equal(Formatted, File.ReadAllText(path));
    }

    [Fact]
    public void NoProjectNeeded()
    {
        Write("loose.cs", Unformatted);

        Assert.Empty(Directory.GetFiles(directory, "*.*proj"));
        Assert.Equal(0, Run(directory).Code);
    }

    [Fact]
    public void ParseErrorLeavesFileUntouched()
    {
        string broken = Write("broken.cs", "class C { void M( }\n");
        string good = Write("good.cs", Unformatted);
        byte[] before = File.ReadAllBytes(broken);

        var (code, _, error) = Run(directory);

        Assert.Equal(2, code);
        Assert.Equal(before, File.ReadAllBytes(broken));
        Assert.Contains("broken.cs", error, StringComparison.Ordinal);
        Assert.Equal(Formatted, File.ReadAllText(good));
    }

    [Fact]
    public void SelfCheckBlocksBadOutput()
    {
        string path = Write("a.cs", "class Foo { }\n");
        byte[] before = File.ReadAllBytes(path);
        var output = new StringWriter();
        var error = new StringWriter();

        int code = CliApp.Run([path], output, error, TestFormatters.RenamesIdentifier, new DiskFileStore());

        Assert.Equal(2, code);
        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Contains("a.cs", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("TreePreserving", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WritesAreAtomic()
    {
        string first = Write("1.cs", Unformatted);
        string second = Write("2.cs", Unformatted);
        var store = new FailingStore(second);
        var output = new StringWriter();
        var error = new StringWriter();

        int code = CliApp.Run([first, second], output, error, new RoslynFormatter(), store);

        Assert.Equal(2, code);
        Assert.Equal(Formatted, File.ReadAllText(first));
        Assert.Equal(Unformatted, File.ReadAllText(second));
        Assert.Contains("2.cs", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ADiskWriteLeavesNoTemporaryFile()
    {
        Write("a.cs", Unformatted);

        Run(directory);

        Assert.Equal(["a.cs"], Directory.GetFiles(directory).Select(Path.GetFileName), StringComparer.Ordinal);
    }

    [Fact]
    public void AnAlreadyFormattedFileIsNotWritten()
    {
        string path = Write("a.cs", Formatted);
        var store = new RecordingStore();

        int code = CliApp.Run([path], new StringWriter(), new StringWriter(), new RoslynFormatter(), store);

        Assert.Equal(0, code);
        Assert.Empty(store.Writes);
    }

    [Fact]
    public void ABomIsKeptAndLineEndingsAreNotConverted()
    {
        string path = Path.Combine(directory, "crlf.cs");
        byte[] bom = [0xEF, 0xBB, 0xBF];
        File.WriteAllBytes(path, [.. bom, .. Encoding.UTF8.GetBytes("class   A\r\n{\r\n    int   x ;\r\n}\r\n")]);

        Assert.Equal(0, Run(path).Code);

        byte[] after = File.ReadAllBytes(path);
        Assert.True(after.AsSpan().StartsWith(bom));
        Assert.Equal("class A\r\n{\r\n    int x;\r\n}\r\n", Encoding.UTF8.GetString(after, 3, after.Length - 3));
    }

    [Fact]
    public void AFileThatIsNotUtf8IsLeftUntouched()
    {
        string path = Path.Combine(directory, "latin1.cs");
        byte[] bytes = [.. Encoding.UTF8.GetBytes("class A { string s = \""), 0xE9, .. Encoding.UTF8.GetBytes("\"; }\n")];
        File.WriteAllBytes(path, bytes);

        var (code, _, error) = Run(path);

        Assert.Equal(2, code);
        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.Contains("UTF-8", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingPathIsAnError()
    {
        var (code, _, error) = Run(Path.Combine(directory, "missing.cs"));

        Assert.Equal(2, code);
        Assert.Contains("missing.cs", error, StringComparison.Ordinal);
    }

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int code = CliApp.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    private string Write(string relative, string contents)
    {
        string path = Path.Combine(directory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    private sealed class RecordingStore : IFileStore
    {
        public List<string> Writes { get; } = [];

        public byte[] Read(string path) => File.ReadAllBytes(path);

        public void ReplaceAtomically(string path, byte[] contents) => Writes.Add(path);
    }

    private sealed class FailingStore(string failingPath) : IFileStore
    {
        public byte[] Read(string path) => File.ReadAllBytes(path);

        public void ReplaceAtomically(string path, byte[] contents)
        {
            if (string.Equals(path, failingPath, StringComparison.Ordinal))
            {
                throw new IOException("disk full");
            }

            new DiskFileStore().ReplaceAtomically(path, contents);
        }
    }
}
