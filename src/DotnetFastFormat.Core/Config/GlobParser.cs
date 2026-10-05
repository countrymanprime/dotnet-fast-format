using System.Globalization;
using System.Text;

namespace DotnetFastFormat.Core.Config;

/// <summary>
/// Turns a section name into flat token sequences, one for each way its <c>{a,b}</c> alternatives can be chosen.
/// The dialect is the one in the specification's "Glob Expressions" chapter (ADR 0012). Anything that does not form
/// a complete construct (an unclosed <c>[</c> or <c>{</c>, a single alternative, a range whose bounds are not
/// increasing) is literal text.
/// </summary>
internal sealed class GlobParser
{
    /// <summary>The longest pattern parsed; the specification asks for 1,024.</summary>
    public const int MaxLength = 4096;

    /// <summary>The most flat sequences one pattern may expand to.</summary>
    public const int MaxVariants = 1024;

    private const int MaxDepth = 32;

    private bool tooComplex;

    private GlobParser()
    {
    }

    /// <summary>Parses <paramref name="pattern"/>.</summary>
    /// <param name="pattern">The section name.</param>
    /// <param name="hasSeparator">Whether the pattern contains a <c>/</c> outside square brackets, so it is relative to the configuration directory.</param>
    /// <returns>The flat sequences, or <see langword="null"/> when the pattern is too long, nests too deeply or expands to too many sequences.</returns>
    public static List<GlobToken[]>? Parse(string pattern, out bool hasSeparator)
    {
        hasSeparator = false;
        if (pattern.Length > MaxLength)
        {
            return null;
        }

        hasSeparator = HasSeparator(pattern);
        var parser = new GlobParser();
        List<List<GlobToken>> sequences = parser.Sequence(pattern, 0);
        if (parser.tooComplex)
        {
            return null;
        }

        var result = new List<GlobToken[]>(sequences.Count);
        foreach (List<GlobToken> sequence in sequences)
        {
            bool leadingSlash = hasSeparator && sequence.Count > 0 && sequence[0] is { Kind: GlobTokenKind.Literal, Char: '/' };
            result.Add(leadingSlash ? [.. sequence.Skip(1)] : [.. sequence]);
        }

        return result;
    }

    private static bool HasSeparator(string pattern)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            switch (pattern[i])
            {
                case '/':
                    return true;
                case '\\':
                    if (i + 1 < pattern.Length && pattern[i + 1] == '/')
                    {
                        return true;
                    }

                    i++;
                    break;
                case '[':
                    int end = ClassEnd(pattern, i);
                    if (end > 0)
                    {
                        i = end;
                    }

                    break;
            }
        }

        return false;
    }

    /// <summary>Finds the <c>]</c> that closes the <c>[</c> at <paramref name="open"/>.</summary>
    /// <returns>Its index, or -1 when there is none or the set would be empty.</returns>
    private static int ClassEnd(string text, int open)
    {
        int start = open + 1;
        if (start < text.Length && text[start] == '!')
        {
            start++;
        }

        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == '\\')
            {
                i++;
            }
            else if (text[i] == ']')
            {
                return i > start ? i : -1;
            }
        }

        return -1;
    }

    private static GlobToken ClassToken(string text, int open, int close)
    {
        bool negated = text[open + 1] == '!';
        var items = new List<char>();
        var dashAt = new List<bool>();
        for (int i = open + (negated ? 2 : 1); i < close; i++)
        {
            bool escaped = text[i] == '\\' && i + 1 < close;
            if (escaped)
            {
                i++;
            }

            items.Add(text[i]);
            dashAt.Add(!escaped && text[i] == '-');
        }

        var set = new StringBuilder();
        for (int i = 0; i < items.Count; i++)
        {
            bool isRange = i + 2 < items.Count && dashAt[i + 1] && !dashAt[i];
            set.Append(items[i]).Append(isRange ? items[i + 2] : items[i]);
            i += isRange ? 2 : 0;
        }

        return new GlobToken(GlobTokenKind.Class, Set: set.ToString(), Negated: negated);
    }

    /// <summary>Finds the <c>}</c> that closes the <c>{</c> at <paramref name="open"/>, skipping nested braces, escapes and sets.</summary>
    private static int BraceEnd(string text, int open)
    {
        int depth = 1;
        for (int i = open + 1; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '\\':
                    i++;
                    break;
                case '[':
                    int end = ClassEnd(text, i);
                    if (end > 0)
                    {
                        i = end;
                    }

                    break;
                case '{':
                    depth++;
                    break;
                case '}':
                    if (--depth == 0)
                    {
                        return i;
                    }

                    break;
            }
        }

        return -1;
    }

    /// <summary>Splits the inside of a brace group at the commas that are not nested, escaped or inside a set.</summary>
    private static List<string> SplitAlternatives(string inner)
    {
        var parts = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < inner.Length; i++)
        {
            switch (inner[i])
            {
                case '\\':
                    i++;
                    break;
                case '[':
                    int end = ClassEnd(inner, i);
                    if (end > 0)
                    {
                        i = end;
                    }

                    break;
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(inner[start..i]);
                    start = i + 1;
                    break;
            }
        }

        parts.Add(inner[start..]);
        return parts;
    }

    private static bool TryNumberRange(string inner, out GlobToken token)
    {
        token = default;
        int dots = inner.IndexOf("..", StringComparison.Ordinal);
        if (dots < 0 || !IsInteger(inner.AsSpan(0, dots), out long low) || !IsInteger(inner.AsSpan(dots + 2), out long high) || low >= high)
        {
            return false;
        }

        token = new GlobToken(GlobTokenKind.NumberRange, Low: low, High: high);
        return true;
    }

    private static bool IsInteger(ReadOnlySpan<char> text, out long value)
    {
        value = 0;
        ReadOnlySpan<char> digits = text.StartsWith("-", StringComparison.Ordinal) ? text[1..] : text;
        foreach (char c in digits)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return !digits.IsEmpty && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static void Append(List<List<GlobToken>> sequences, GlobToken token)
    {
        foreach (List<GlobToken> sequence in sequences)
        {
            sequence.Add(token);
        }
    }

    private List<List<GlobToken>> Sequence(string text, int depth)
    {
        var sequences = new List<List<GlobToken>> { new() };
        if (depth > MaxDepth)
        {
            tooComplex = true;
            return sequences;
        }

        for (int i = 0; i < text.Length && !tooComplex; i++)
        {
            char c = text[i];
            switch (c)
            {
                case '\\' when i + 1 < text.Length:
                    Append(sequences, new GlobToken(GlobTokenKind.Literal, text[++i]));
                    break;
                case '*':
                    int stars = 1;
                    while (i + 1 < text.Length && text[i + 1] == '*')
                    {
                        stars++;
                        i++;
                    }

                    Append(sequences, new GlobToken(stars > 1 ? GlobTokenKind.StarStar : GlobTokenKind.Star));
                    break;
                case '?':
                    Append(sequences, new GlobToken(GlobTokenKind.AnyChar));
                    break;
                case '[' when ClassEnd(text, i) is var close and > 0:
                    Append(sequences, ClassToken(text, i, close));
                    i = close;
                    break;
                case '{' when BraceEnd(text, i) is var end and > 0 && TryBrace(text, i, end, depth) is { } group:
                    sequences = Cross(sequences, group);
                    i = end;
                    break;
                default:
                    Append(sequences, new GlobToken(GlobTokenKind.Literal, c));
                    break;
            }
        }

        return sequences;
    }

    /// <summary>Reads the group <c>{...}</c> from <paramref name="open"/> to <paramref name="close"/>.</summary>
    /// <returns>The alternatives, or <see langword="null"/> when it is not a group (one alternative, no range) and the brace is literal.</returns>
    private List<List<GlobToken>>? TryBrace(string text, int open, int close, int depth)
    {
        string inner = text[(open + 1)..close];
        if (TryNumberRange(inner, out GlobToken range))
        {
            return [[range]];
        }

        List<string> parts = SplitAlternatives(inner);
        if (parts.Count < 2)
        {
            return null;
        }

        var alternatives = new List<List<GlobToken>>();
        foreach (string part in parts)
        {
            alternatives.AddRange(Sequence(part, depth + 1));
            if (alternatives.Count > MaxVariants || tooComplex)
            {
                tooComplex = true;
                break;
            }
        }

        return alternatives;
    }

    private List<List<GlobToken>> Cross(List<List<GlobToken>> sequences, List<List<GlobToken>> alternatives)
    {
        if (tooComplex || (long)sequences.Count * alternatives.Count > MaxVariants)
        {
            tooComplex = true;
            return sequences;
        }

        var result = new List<List<GlobToken>>(sequences.Count * alternatives.Count);
        foreach (List<GlobToken> head in sequences)
        {
            foreach (List<GlobToken> tail in alternatives)
            {
                result.Add([.. head, .. tail]);
            }
        }

        return result;
    }
}
