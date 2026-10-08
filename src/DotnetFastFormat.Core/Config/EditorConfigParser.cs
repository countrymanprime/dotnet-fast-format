namespace DotnetFastFormat.Core.Config;

/// <summary>
/// Reads the text of an <c>.editorconfig</c> file as the <see href="https://spec.editorconfig.org/">specification</see>
/// defines it (ADR 0012): one line at a time, trimmed; blank lines and lines starting with <c>;</c> or <c>#</c> are
/// ignored; <c>[...]</c> starts a section; anything else with an <c>=</c> is a pair; there are no inline comments.
/// </summary>
internal static class EditorConfigParser
{
    private const char Bom = '﻿';

    /// <summary>Parses <paramref name="text"/>. This never throws: a line that is not valid is skipped and reported.</summary>
    /// <param name="text">The file's text, with or without a byte order mark.</param>
    /// <returns>The parsed file.</returns>
    public static EditorConfigFile Parse(string text)
    {
        var sections = new List<EditorConfigSection>();
        var problems = new List<EditorConfigProblem>();
        bool isRoot = false;

        ReadOnlySpan<char> rest = text.AsSpan();
        if (!rest.IsEmpty && rest[0] == Bom)
        {
            rest = rest[1..];
        }

        int lineNumber = 0;
        while (!rest.IsEmpty)
        {
            int end = rest.IndexOfAny('\r', '\n');
            ReadOnlySpan<char> line = end < 0 ? rest : rest[..end];
            lineNumber++;
            rest = end < 0 ? default : rest[SkipLineBreak(rest, end)..];

            ReadOnlySpan<char> trimmed = line.Trim();
            if (trimmed.IsEmpty || trimmed[0] is ';' or '#')
            {
                continue;
            }

            if (trimmed[0] == '[' && trimmed[^1] == ']')
            {
                sections.Add(new EditorConfigSection(trimmed[1..^1].ToString(), lineNumber));
                continue;
            }

            int equals = trimmed.IndexOf('=');
            ReadOnlySpan<char> key = equals < 0 ? default : trimmed[..equals].TrimEnd();
            if (key.IsEmpty)
            {
                problems.Add(new EditorConfigProblem(lineNumber, "not a section header, a key = value pair or a comment"));
                continue;
            }

            string value = trimmed[(equals + 1)..].Trim().ToString();
            if (sections.Count > 0)
            {
                sections[^1].Add(new EditorConfigPair(key.ToString().ToLowerInvariant(), value, lineNumber));
            }
            else if (key.Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                isRoot = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        return new EditorConfigFile(isRoot, sections, problems);
    }

    private static int SkipLineBreak(ReadOnlySpan<char> text, int index) =>
        text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n' ? index + 2 : index + 1;
}
