using System.Text.RegularExpressions;
using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

/// <summary>Formatters used to prove the invariant helper accepts good output and rejects bad output.</summary>
internal static partial class TestFormatters
{
    public static IFormatter Identity { get; } = new Delegate(s => s);

    /// <summary>Appends a newline on every run, so output keeps changing.</summary>
    public static IFormatter NonIdempotent { get; } = new Delegate(s => s + "\n");

    /// <summary>Renames an identifier, changing the syntax tree.</summary>
    public static IFormatter RenamesIdentifier { get; } = new Delegate(s => s.Replace("Foo", "Bar", StringComparison.Ordinal));

    /// <summary>Deletes every comment.</summary>
    public static IFormatter DropsComments { get; } = new Delegate(s => CommentPattern().Replace(s, string.Empty));

    /// <summary>Moves the first line (a comment in the fixtures) to the end of the file.</summary>
    public static IFormatter MovesComment { get; } = new Delegate(s =>
    {
        int newline = s.IndexOf('\n', StringComparison.Ordinal);
        return s[(newline + 1)..] + s[..(newline + 1)];
    });

    /// <summary>Appends an unmatched brace, producing output that does not parse.</summary>
    public static IFormatter InvalidOutput { get; } = new Delegate(s => s + "}");

    [GeneratedRegex(@"//[^\n]*|/\*.*?\*/", RegexOptions.Singleline, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CommentPattern();

    internal sealed class Delegate(Func<string, string> format) : IFormatter
    {
        public string Format(string source) => format(source);

        public string Format(string source, FormatOptions options) => format(source);
    }
}
