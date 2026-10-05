namespace DotnetFastFormat.Core.Config;

/// <summary>One <c>key = value</c> that applies to a file, with where it was written.</summary>
/// <param name="Value">The value, in the case it was written.</param>
/// <param name="Path">The path of the <c>.editorconfig</c> file.</param>
/// <param name="Line">The line number in that file.</param>
internal readonly record struct Assignment(string Value, string Path, int Line);
