namespace DotnetFastFormat.Core.Config;

/// <summary>The <c>.editorconfig</c> files that can apply to the files of one directory.</summary>
/// <param name="Files">The files that were found, nearest first, ending at a file with <c>root = true</c> or at the top of the search.</param>
/// <param name="Unreadable">One message for each <c>.editorconfig</c> on the way that could not be read and was skipped.</param>
internal sealed record ConfigChain(IReadOnlyList<LoadedEditorConfig> Files, IReadOnlyList<string> Unreadable);
