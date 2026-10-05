namespace DotnetFastFormat.Core.Config;

/// <summary>A line of an <c>.editorconfig</c> file that is not blank, a comment, a section header or a pair.</summary>
/// <param name="Line">The line number, counted from 1.</param>
/// <param name="Message">What is wrong with it.</param>
internal readonly record struct EditorConfigProblem(int Line, string Message);
