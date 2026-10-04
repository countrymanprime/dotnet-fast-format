namespace DotnetFastFormat.Benchmarks;

/// <summary>The names of the tools the harness can measure.</summary>
internal static class ToolNames
{
    /// <summary><c>dotnet format whitespace --folder</c>: the whitespace pass, no MSBuild project load.</summary>
    public const string DotnetFormatWhitespace = "dotnet-format-whitespace";

    /// <summary><c>dotnet format</c> on the repository's solution: loads the projects through MSBuild.</summary>
    public const string DotnetFormat = "dotnet-format";

    /// <summary>CSharpier.</summary>
    public const string CSharpier = "csharpier";

    /// <summary>Every tool name, in report order.</summary>
    public static IReadOnlyList<string> All { get; } = [DotnetFormatWhitespace, DotnetFormat, CSharpier];
}
