using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Core;

/// <summary>The one place source text is parsed, so the formatter and the output check agree on the language version.</summary>
public static class SourceParser
{
    // A tree is immutable, so the formatter and the output check can share the tree of the same string instance: the check
    // would otherwise parse the input a second time. The entry lives as long as the string does.
    private static readonly ConditionalWeakTable<string, SyntaxTree> Parsed = new();

    /// <summary>Gets the parse options used for every parse: the newest language version, so no valid syntax is rejected.</summary>
    public static CSharpParseOptions Options { get; } = new(LanguageVersion.Preview);

    /// <summary>Parses <paramref name="source"/> with <see cref="Options"/>.</summary>
    /// <param name="source">The C# source text. Parsing the same string instance again returns the same tree.</param>
    /// <returns>The syntax tree, which may contain error diagnostics.</returns>
    public static SyntaxTree Parse(string source) => Parsed.GetValue(source, text => CSharpSyntaxTree.ParseText(text, Options));
}
