using DotnetFastFormat.Core.Layout;
using DotnetFastFormat.Core.Printers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core;

/// <summary>
/// Formats C# by parsing it with Roslyn and printing a doc. Nodes the formatter does not handle yet
/// are printed verbatim (ADR 0008).
/// </summary>
public sealed class RoslynFormatter : IFormatter
{
    private const int MaxDepth = 1000;
    private const int StackBytes = 64 * 1024 * 1024;
    private const int NoLimit = int.MaxValue / 2;

    /// <summary>Formats <paramref name="source"/> with <see cref="FormatOptions.Default"/>.</summary>
    /// <param name="source">C# source text.</param>
    /// <returns>The formatted text.</returns>
    /// <exception cref="SyntaxErrorException">The input has syntax errors.</exception>
    /// <exception cref="FormatterException">The input is too deeply nested to process.</exception>
    public string Format(string source) => Format(source, FormatOptions.Default);

    /// <inheritdoc/>
    /// <exception cref="SyntaxErrorException">The input has syntax errors.</exception>
    /// <exception cref="FormatterException">The input is too deeply nested to process.</exception>
    public string Format(string source, FormatOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
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
                    result = FormatCore(source, options);
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

    private static string FormatCore(string source, FormatOptions options)
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
            string newLine = options.EndOfLine switch
            {
                LineEnding.Lf => "\n",
                LineEnding.CrLf => "\r\n",
                _ => DominantNewLine(source),
            };
            var printOptions = new DocPrintOptions(
                options.MaxLineLength ?? NoLimit,
                options.IndentSize,
                newLine,
                options.IndentStyle == IndentStyle.Tab,
                options.TabWidth);

            string text;
            if (VerbatimPolicy.KeepsWholeFile(root))
            {
                string body = source.Trim();
                text = body.Length == 0
                    ? string.Empty
                    : DocPrinter.Print(Docs.Concat(Docs.Verbatim(body), Docs.HardLine), printOptions);
            }
            else
            {
                text = DocPrinter.Print(CompilationUnitPrinter.Print(root), printOptions);
            }

            return Finish(text, options, newLine);
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new FormatterException("The input is nested too deeply to format.", ex);
        }
    }

    private static string Finish(string text, FormatOptions options, string newLine)
    {
        if (options.EndOfLine is not null)
        {
            text = LineEndings.Convert(text, newLine);
        }

        if (!options.InsertFinalNewline)
        {
            if (text.EndsWith("\r\n", StringComparison.Ordinal))
            {
                return text[..^2];
            }

            return text.EndsWith('\n') ? text[..^1] : text;
        }

        return text;
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
