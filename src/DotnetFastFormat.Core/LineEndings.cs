using System.Text;
using Microsoft.CodeAnalysis;

namespace DotnetFastFormat.Core;

/// <summary>Converts the line terminators of formatted text to one kind, without touching the text inside tokens (ADR 0012).</summary>
internal static class LineEndings
{
    /// <summary>
    /// Rewrites every line terminator in whitespace, comments, directives and disabled text. A terminator inside a
    /// token, such as a multi-line string literal, is part of the program's value and stays as written.
    /// </summary>
    /// <param name="text">Formatted C# text that parses without errors.</param>
    /// <param name="newLine">The wanted terminator, <c>\n</c> or <c>\r\n</c>.</param>
    /// <returns>The converted text; <paramref name="text"/> itself when nothing differs.</returns>
    public static string Convert(string text, string newLine)
    {
        if (!HasOther(text, newLine))
        {
            return text;
        }

        var result = new StringBuilder(text.Length + 16);
        foreach (SyntaxToken token in SourceParser.Parse(text).GetRoot().DescendantTokens())
        {
            AppendTrivia(result, token.LeadingTrivia, newLine);
            result.Append(token.Text);
            AppendTrivia(result, token.TrailingTrivia, newLine);
        }

        return result.ToString();
    }

    private static void AppendTrivia(StringBuilder result, SyntaxTriviaList trivia, string newLine)
    {
        foreach (SyntaxTrivia piece in trivia)
        {
            ReadOnlySpan<char> text = piece.ToFullString();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\r')
                {
                    result.Append(newLine);
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                }
                else if (c == '\n')
                {
                    result.Append(newLine);
                }
                else
                {
                    result.Append(c);
                }
            }
        }
    }

    private static bool HasOther(string text, string newLine)
    {
        bool crlf = newLine.Length == 2;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r')
            {
                bool followedByLf = i + 1 < text.Length && text[i + 1] == '\n';
                if (!crlf || !followedByLf)
                {
                    return true;
                }

                i++;
            }
            else if (c == '\n' && crlf)
            {
                return true;
            }
        }

        return false;
    }
}
