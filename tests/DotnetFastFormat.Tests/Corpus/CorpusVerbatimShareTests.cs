using System.Globalization;
using System.Text;
using DotnetFastFormat.Core;
using DotnetFastFormat.Core.Layout;
using DotnetFastFormat.Core.Printers;
using DotnetFastFormat.Corpus;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Tests.Corpus;

/// <summary>
/// Measures how much of each pinned repository is still copied as written (ADR 0008): the characters of verbatim
/// text in the document the printers build, against the characters of the files. Code that is copied (a node or an
/// expression with no printer, attribute lists, a whole file kept as written) is counted apart from multi-line strings,
/// block comments and the text of disabled preprocessor branches, which are always copied. The result is written to
/// <c>.bench/verbatim-share.md</c>; the test only guards against a collapse of the printers.
/// </summary>
[Trait("Category", "Slow")]
public class CorpusVerbatimShareTests
{
    private const double MaxCodeShare = 0.5;

    [Fact]
    public void MostOfTheCorpusIsPrintedAndNotCopied()
    {
        var table = new StringBuilder("| Repository | Files | Characters | Code copied | Strings spanning lines | Block comments | Disabled text | Code share |\n|---|---|---|---|---|---|---|---|\n");
        var total = new Share(0, 0, 0, 0, 0, 0);
        foreach (CorpusRepository repository in CorpusManifest.Load(CorpusManifest.DefaultPath()))
        {
            string root = CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot()).Path;
            Share share = Measure(root);
            total = total.Add(share);
            table.AppendLine(Row(repository.Name, share));
        }

        table.AppendLine(Row("all", total));
        string directory = Path.Combine(RepositoryRoot.Find(), ".bench");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "verbatim-share.md"), table.ToString());

        Assert.True((double)total.Code / total.Characters < MaxCodeShare, table.ToString());
    }

    private static string Row(string name, Share share) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"| {name} | {share.Files} | {share.Characters} | {share.Code} | {share.Strings} | {share.Comments} | {share.Disabled} | {(share.Characters == 0 ? 0 : 100.0 * share.Code / share.Characters):F1}% |");

    private static Share Measure(string root)
    {
        var share = new Share(0, 0, 0, 0, 0, 0);
        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            SyntaxTree tree = SourceParser.Parse(source);
            if (tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                continue;
            }

            var unit = (CompilationUnitSyntax)tree.GetRoot();
            Share file = VerbatimPolicy.KeepsWholeFile(unit)
                ? WholeFile(unit, source.Length)
                : Walk(CompilationUnitPrinter.Print(unit), source.Length);
            share = share.Add(file);
        }

        return share;
    }

    /// <summary>A file with no code in the parse without symbols (everything is inside <c>#if</c>) is disabled text; any other file kept whole is code.</summary>
    private static Share WholeFile(CompilationUnitSyntax unit, int characters) =>
        !unit.Externs.Any() && !unit.Usings.Any() && !unit.AttributeLists.Any() && !unit.Members.Any()
            ? new Share(1, characters, 0, 0, 0, characters)
            : new Share(1, characters, characters, 0, 0, 0);

    private static Share Walk(Doc root, int characters)
    {
        long code = 0;
        long strings = 0;
        long comments = 0;
        long disabled = 0;
        var pending = new Stack<(Doc Doc, bool Disabled)>();
        pending.Push((root, false));
        while (pending.Count > 0)
        {
            (Doc doc, bool inDisabled) = pending.Pop();
            switch (doc)
            {
                case VerbatimDoc verbatim when inDisabled:
                    disabled += verbatim.Value.Length;
                    break;
                case VerbatimDoc verbatim when !verbatim.ForcesBreak && verbatim.Value.AsSpan().IndexOfAny('\r', '\n') >= 0:
                    strings += verbatim.Value.Length;
                    break;
                case VerbatimDoc verbatim when verbatim.Value.StartsWith("/*", StringComparison.Ordinal):
                    comments += verbatim.Value.Length;
                    break;
                case VerbatimDoc verbatim:
                    code += verbatim.Value.Length;
                    break;
                case ConcatDoc concat:
                    Push(pending, concat.Parts, inDisabled);
                    break;
                case FillDoc fill:
                    Push(pending, fill.Parts, inDisabled);
                    break;
                case GroupDoc group:
                    pending.Push((group.Contents, inDisabled));
                    break;
                case IndentDoc indent:
                    pending.Push((indent.Contents, inDisabled));
                    break;
                case ColumnZeroDoc columnZero:
                    pending.Push((columnZero.Contents, true));
                    break;
                case IfBreakDoc ifBreak:
                    pending.Push((ifBreak.FlatContents, inDisabled));
                    break;
                case ConditionalGroupDoc conditional:
                    pending.Push((conditional.States[0], inDisabled));
                    break;
            }
        }

        return new Share(1, characters, code, strings, comments, disabled);
    }

    private static void Push(Stack<(Doc Doc, bool Disabled)> pending, IReadOnlyList<Doc> parts, bool inDisabled)
    {
        foreach (Doc part in parts)
        {
            pending.Push((part, inDisabled));
        }
    }

    private sealed record Share(int Files, long Characters, long Code, long Strings, long Comments, long Disabled)
    {
        public Share Add(Share other) =>
            new(Files + other.Files, Characters + other.Characters, Code + other.Code, Strings + other.Strings, Comments + other.Comments, Disabled + other.Disabled);
    }
}
