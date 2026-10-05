using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core;

/// <summary>
/// Formats C# by parsing it with Roslyn and printing a doc. Nodes the formatter does not handle yet
/// are printed verbatim (ADR 0008), so today every file is.
/// </summary>
public sealed class RoslynFormatter : IFormatter
{
    private const int MaxDepth = 1000;
    private const int StackBytes = 64 * 1024 * 1024;

    /// <inheritdoc/>
    /// <exception cref="SyntaxErrorException">The input has syntax errors.</exception>
    /// <exception cref="FormatterException">The input is too deeply nested to process.</exception>
    public string Format(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (MaxBracketDepth(source) > MaxDepth)
        {
            throw new FormatterException($"The input nests brackets more than {MaxDepth} deep, so it is not formatted.");
        }

        // Roslyn's parser recurses; a stack overflow cannot be caught, so give it room and bound the depth above.
        string? result = null;
        Exception? failure = null;
        var worker = new Thread(
            () =>
            {
                try
                {
                    result = FormatCore(source);
                }
                catch (Exception ex) when (ex is FormatterException or InsufficientExecutionStackException)
                {
                    failure = ex;
                }
            },
            StackBytes);
        worker.Start();
        worker.Join();

        return failure switch
        {
            null => result!,
            FormatterException known => throw known,
            _ => throw new FormatterException("The input is nested too deeply to format.", failure),
        };
    }

    private static int MaxBracketDepth(string source)
    {
        int depth = 0;
        int max = 0;
        foreach (char c in source)
        {
            if (c is '(' or '[' or '{')
            {
                max = Math.Max(max, ++depth);
            }
            else if (c is ')' or ']' or '}')
            {
                depth = Math.Max(0, depth - 1);
            }
        }

        return max;
    }

    private static string FormatCore(string source)
    {
        try
        {
            SyntaxTree tree = SourceParser.Parse(source);
            Diagnostic? error = tree.GetDiagnostics().FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            if (error is not null)
            {
                throw new SyntaxErrorException(error.ToString());
            }

            var root = (CompilationUnitSyntax)tree.GetRoot();
            string newLine = DominantNewLine(source);

            // Only whole-file verbatim exists so far; per-node printing arrives with the builders.
            _ = VerbatimPolicy.KeepsWholeFile(root);
            string body = source.Trim('\r', '\n', ' ', '\t');
            if (body.Length == 0)
            {
                return string.Empty;
            }

            return DocPrinter.Print(Docs.Concat(Docs.Verbatim(body), Docs.HardLine), new DocPrintOptions(newLine: newLine));
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new FormatterException("The input is nested too deeply to format.", ex);
        }
    }

    private static string DominantNewLine(string source)
    {
        int crlf = 0;
        int lf = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != '\n')
            {
                continue;
            }

            if (i > 0 && source[i - 1] == '\r')
            {
                crlf++;
            }
            else
            {
                lf++;
            }
        }

        return crlf > lf ? "\r\n" : "\n";
    }
}
