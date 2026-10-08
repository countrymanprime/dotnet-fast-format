using System.Globalization;
using DotnetFastFormat.Cli;
using DotnetFastFormat.Core;
using DotnetFastFormat.Core.Config;
using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests.Corpus;

/// <summary>
/// The real-world check of CFG-001, CFG-002 and CFG-013: the pinned repositories carry their own
/// <c>.editorconfig</c> files (tabs, two-space indents, <c>end_of_line</c>, <c>charset</c>, <c>insert_final_newline</c>),
/// and every file is formatted under the settings that apply to it. Slow tier: it needs the corpus.
/// </summary>
[Trait("Category", "Slow")]
public class CorpusEditorConfigTests
{
    public static TheoryData<string> RepositoryNames() => CorpusFetchTests.RepositoryNames();

    [Theory]
    [MemberData(nameof(RepositoryNames))]
    public void FormattingKeepsEveryInvariantUnderEditorConfig(string name)
    {
        string root = RepositoryPath(name);
        var resolver = new EditorConfigResolver(ReadIfExists, ceiling: root);
        var formatter = new RoslynFormatter();
        var failures = new List<string>();
        var optionSets = new Dictionary<string, int>(StringComparer.Ordinal);
        var warnings = new HashSet<string>(StringComparer.Ordinal);
        int files = 0;
        int rejected = 0;

        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            files++;
            ResolvedSettings settings = resolver.Resolve(path);
            warnings.UnionWith(settings.Warnings);
            string key = Describe(settings.Options);
            optionSets[key] = optionSets.GetValueOrDefault(key) + 1;
            try
            {
                Invariants.FormatAndCheck(formatter, File.ReadAllText(path), settings.Options);
            }
            catch (FormatterException)
            {
                rejected++;
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetRelativePath(root, path)} [{key}]: {ex.GetType().Name}: {ex.Message}");
            }
        }

        string summary = $"{name}: {files} files, {rejected} rejected, {optionSets.Count} option sets ({string.Join("; ", optionSets.OrderByDescending(p => p.Value).Select(p => $"{p.Key} x{p.Value}"))}), {warnings.Count} warnings";
        TestContext.Current.SendDiagnosticMessage(summary);
        foreach (string warning in warnings)
        {
            TestContext.Current.SendDiagnosticMessage($"{name}: warning: {warning}");
        }

        Assert.True(files > 0, $"{name} has no C# files.");
        Assert.True(failures.Count == 0, $"{failures.Count} of {files} files failed ({rejected} rejected):\n{string.Join('\n', failures.Take(25))}");
    }

    /// <summary>
    /// The corpus' own configurations never set tabs or a two-space indent for C#, so the same files are also formatted
    /// under settings chosen to be as unlike the defaults as possible.
    /// </summary>
    [Theory]
    [MemberData(nameof(RepositoryNames))]
    public void FormattingKeepsEveryInvariantUnderUnusualSettings(string name)
    {
        string root = RepositoryPath(name);
        var formatter = new RoslynFormatter();
        FormatOptions[] settings =
        [
            new() { IndentStyle = IndentStyle.Tab, IndentSize = 4, TabWidth = 8, MaxLineLength = 80, EndOfLine = LineEnding.Lf },
            new() { IndentSize = 2, MaxLineLength = 60, EndOfLine = LineEnding.CrLf, InsertFinalNewline = false },
            new() { IndentStyle = IndentStyle.Tab, IndentSize = 3, TabWidth = 3, MaxLineLength = null, InsertFinalNewline = false },
        ];
        var failures = new List<string>();
        int files = 0;

        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            files++;
            string source = File.ReadAllText(path);
            foreach (FormatOptions options in settings)
            {
                try
                {
                    Invariants.FormatAndCheck(formatter, source, options);
                }
                catch (FormatterException)
                {
                    // A file with syntax errors is rejected, as in the other corpus tests.
                    break;
                }
                catch (Exception ex)
                {
                    failures.Add($"{Path.GetRelativePath(root, path)} [{Describe(options)}]: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        Assert.True(files > 0, $"{name} has no C# files.");
        Assert.True(failures.Count == 0, $"{failures.Count} failures in {files} files:\n{string.Join('\n', failures.Take(25))}");
    }

    [Theory]
    [MemberData(nameof(RepositoryNames))]
    public void TheCommandLineFormatsACopyAndASecondRunChangesNothing(string name)
    {
        string source = RepositoryPath(name);
        string temporary = Directory.CreateTempSubdirectory("fast-format-corpus-").FullName;
        try
        {
            // The copy sits below a root = true file, so nothing above the repository's own files applies.
            File.WriteAllText(Path.Combine(temporary, ".editorconfig"), "root = true\n");
            string copy = Path.Combine(temporary, name);
            CopyConfigurationAndSources(source, copy);

            (int firstCode, string firstOutput, string firstError) = Run(copy);
            (int secondCode, string secondOutput, string secondError) = Run(copy);

            TestContext.Current.SendDiagnosticMessage($"{name}: first run: {firstOutput.Trim()}");
            Assert.DoesNotContain("internal error", firstError, StringComparison.Ordinal);
            Assert.DoesNotContain("internal error", secondError, StringComparison.Ordinal);
            Assert.Contains("Formatted 0 file(s)", secondOutput, StringComparison.Ordinal);
            Assert.Equal(firstCode, secondCode);
            Assert.Equal(firstError, secondError);
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static string RepositoryPath(string name)
    {
        CorpusRepository repository = CorpusManifest.Load(CorpusManifest.DefaultPath()).Single(r => string.Equals(r.Name, name, StringComparison.Ordinal));
        return CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot()).Path;
    }

    private static string? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    private static string Describe(FormatOptions options) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{options.IndentStyle}/{options.IndentSize}/{options.TabWidth}/{options.MaxLineLength?.ToString(CultureInfo.InvariantCulture) ?? "off"}/{options.EndOfLine?.ToString() ?? "dominant"}/{(options.InsertFinalNewline ? "nl" : "no-nl")}/{options.ByteOrderMark}");

    private static void CopyConfigurationAndSources(string from, string to)
    {
        foreach (string path in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(path);
            if (!name.EndsWith(".cs", StringComparison.Ordinal) && !string.Equals(name, ".editorconfig", StringComparison.Ordinal))
            {
                continue;
            }

            string target = Path.Combine(to, Path.GetRelativePath(from, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
        }
    }

    private static (int Code, string Out, string Err) Run(string directory)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int code = CliApp.Run([directory], output, error);
        return (code, output.ToString(), error.ToString());
    }
}
