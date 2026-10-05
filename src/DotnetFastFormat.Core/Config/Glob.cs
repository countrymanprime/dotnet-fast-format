using System.Globalization;

namespace DotnetFastFormat.Core.Config;

/// <summary>
/// A section name compiled for matching (ADR 0012). A name with a <c>/</c> outside square brackets is relative to the
/// directory of its <c>.editorconfig</c> and a leading <c>/</c> is ignored; any other name matches a path from the
/// start of any of its path segments, which is the same as matching at any depth below that directory.
/// Matching is case-sensitive and is bounded: a failed state is never explored twice.
/// </summary>
internal sealed class Glob
{
    private readonly GlobToken[][] variants;
    private readonly bool anchored;

    private Glob(GlobToken[][] variants, bool anchored)
    {
        this.variants = variants;
        this.anchored = anchored;
    }

    /// <summary>Compiles a section name.</summary>
    /// <param name="pattern">The text between the brackets of a section header.</param>
    /// <returns>The glob, or <see langword="null"/> when the name is too long, too deeply nested or expands to too many alternatives.</returns>
    public static Glob? Parse(string pattern)
    {
        List<GlobToken[]>? sequences = GlobParser.Parse(pattern, out bool hasSeparator);
        return sequences is null ? null : new Glob([.. sequences], hasSeparator);
    }

    /// <summary>Tests a file path.</summary>
    /// <param name="relativePath">The file's path relative to the directory of the <c>.editorconfig</c>, using <c>/</c> as the separator.</param>
    /// <returns><see langword="true"/> when the whole path matches.</returns>
    public bool IsMatch(string relativePath)
    {
        foreach (GlobToken[] tokens in variants)
        {
            if (MatchVariant(tokens, relativePath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSeveralWildcards(GlobToken[] tokens)
    {
        int count = 0;
        foreach (GlobToken token in tokens)
        {
            if (token.IsWildcard && ++count > 1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool InSet(GlobToken token, char c)
    {
        bool found = false;
        for (int i = 0; i + 1 < token.Set.Length && !found; i += 2)
        {
            found = c >= token.Set[i] && c <= token.Set[i + 1];
        }

        return token.Negated ? !found && c != '/' : found;
    }

    private static bool MatchNumber(GlobToken[] tokens, int index, string path, int position, ref HashSet<long>? failed, bool memoize)
    {
        int digitsStart = position < path.Length && path[position] == '-' ? position + 1 : position;
        int digitsEnd = digitsStart;
        while (digitsEnd < path.Length && path[digitsEnd] is >= '0' and <= '9')
        {
            digitsEnd++;
        }

        for (int end = digitsStart + 1; end <= digitsEnd; end++)
        {
            bool canonical = end - digitsStart == 1 || path[digitsStart] != '0';
            if (canonical
                && long.TryParse(path.AsSpan(position, end - position), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value)
                && value >= tokens[index].Low
                && value <= tokens[index].High
                && MatchFrom(tokens, index + 1, path, end, ref failed, memoize))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchWildcard(GlobToken[] tokens, int index, string path, int position, ref HashSet<long>? failed, bool memoize)
    {
        long key = ((long)index << 32) | (uint)position;
        if (memoize && failed is not null && failed.Contains(key))
        {
            return false;
        }

        GlobToken token = tokens[index];
        bool matched = false;
        if (token.Kind == GlobTokenKind.NumberRange)
        {
            matched = MatchNumber(tokens, index, path, position, ref failed, memoize);
        }
        else
        {
            for (int end = position; !matched; end++)
            {
                matched = MatchFrom(tokens, index + 1, path, end, ref failed, memoize);
                if (end >= path.Length || (token.Kind == GlobTokenKind.Star && path[end] == '/'))
                {
                    break;
                }
            }
        }

        if (!matched && memoize)
        {
            (failed ??= []).Add(key);
        }

        return matched;
    }

    private static bool MatchFrom(GlobToken[] tokens, int index, string path, int position, ref HashSet<long>? failed, bool memoize)
    {
        for (; index < tokens.Length; index++, position++)
        {
            GlobToken token = tokens[index];
            if (token.IsWildcard)
            {
                return MatchWildcard(tokens, index, path, position, ref failed, memoize);
            }

            bool ok = position < path.Length && token.Kind switch
            {
                GlobTokenKind.Literal => path[position] == token.Char,
                GlobTokenKind.AnyChar => path[position] != '/',
                _ => InSet(token, path[position]),
            };
            if (!ok)
            {
                return false;
            }
        }

        return position == path.Length;
    }

    private bool MatchVariant(GlobToken[] tokens, string path)
    {
        bool memoize = HasSeveralWildcards(tokens);
        HashSet<long>? failed = null;
        int start = 0;
        while (start <= path.Length)
        {
            if (MatchFrom(tokens, 0, path, start, ref failed, memoize))
            {
                return true;
            }

            int slash = anchored ? -1 : path.IndexOf('/', start);
            if (slash < 0)
            {
                break;
            }

            start = slash + 1;
        }

        return false;
    }
}
