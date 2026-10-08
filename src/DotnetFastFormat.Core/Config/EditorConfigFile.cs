namespace DotnetFastFormat.Core.Config;

/// <summary>A parsed <c>.editorconfig</c> file.</summary>
/// <param name="IsRoot">Whether the preamble sets <c>root = true</c>, so the search for configuration stops here.</param>
/// <param name="Sections">The sections in file order.</param>
/// <param name="Problems">The lines that were skipped because they are not valid.</param>
internal sealed record EditorConfigFile(bool IsRoot, IReadOnlyList<EditorConfigSection> Sections, IReadOnlyList<EditorConfigProblem> Problems);
