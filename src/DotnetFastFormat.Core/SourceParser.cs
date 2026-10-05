using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Core;

/// <summary>The one place source text is parsed, so the formatter and the output check agree on the language version.</summary>
public static class SourceParser
{
    /// <summary>Gets the parse options used for every parse: the newest language version, so no valid syntax is rejected.</summary>
    public static CSharpParseOptions Options { get; } = new(LanguageVersion.Preview);

    /// <summary>Parses <paramref name="source"/> with <see cref="Options"/>.</summary>
    /// <param name="source">The C# source text.</param>
    /// <returns>The syntax tree, which may contain error diagnostics.</returns>
    public static SyntaxTree Parse(string source) => CSharpSyntaxTree.ParseText(source, Options);
}
