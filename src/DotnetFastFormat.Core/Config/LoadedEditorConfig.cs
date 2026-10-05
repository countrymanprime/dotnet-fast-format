namespace DotnetFastFormat.Core.Config;

/// <summary>A parsed <c>.editorconfig</c> with its section globs compiled, kept for the whole run.</summary>
internal sealed class LoadedEditorConfig
{
    private readonly Glob?[] globs;

    /// <summary>Initializes a new instance of the <see cref="LoadedEditorConfig"/> class.</summary>
    /// <param name="filePath">The path of the file.</param>
    /// <param name="parsed">The parsed file.</param>
    public LoadedEditorConfig(string filePath, EditorConfigFile parsed)
    {
        FilePath = filePath;
        Parsed = parsed;
        var warnings = new List<string>();
        globs = new Glob?[parsed.Sections.Count];
        for (int i = 0; i < globs.Length; i++)
        {
            globs[i] = Glob.Parse(parsed.Sections[i].Name);
            if (globs[i] is null)
            {
                warnings.Add($"{filePath}({parsed.Sections[i].Line}): section [{Truncate(parsed.Sections[i].Name)}] is too long or too complex to match; ignored");
            }
        }

        foreach (EditorConfigProblem problem in parsed.Problems)
        {
            warnings.Add($"{filePath}({problem.Line}): {problem.Message}; ignored");
        }

        Warnings = warnings;
    }

    /// <summary>Gets the path of the file.</summary>
    public string FilePath { get; }

    /// <summary>Gets the parsed file.</summary>
    public EditorConfigFile Parsed { get; }

    /// <summary>Gets the messages about the file itself: invalid lines and sections that cannot be matched.</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Returns whether the section at <paramref name="index"/> matches a path.</summary>
    /// <param name="index">The index in <see cref="EditorConfigFile.Sections"/>.</param>
    /// <param name="relativePath">The file's path relative to this file's directory, with <c>/</c> separators.</param>
    /// <returns><see langword="true"/> when the section's glob matches.</returns>
    public bool Matches(int index, string relativePath) => globs[index]?.IsMatch(relativePath) ?? false;

    private static string Truncate(string name) => name.Length <= 60 ? name : name[..60] + "...";
}
