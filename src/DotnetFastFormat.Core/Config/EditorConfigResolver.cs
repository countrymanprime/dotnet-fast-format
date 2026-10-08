using System.Security;

namespace DotnetFastFormat.Core.Config;

/// <summary>
/// Finds the <c>.editorconfig</c> settings for source files (CFG-001 to CFG-005, CFG-010 to CFG-012). For a file it
/// searches its directory and every parent for <c>.editorconfig</c>, nearest first, and stops after a file whose
/// preamble sets <c>root = true</c> (or at an optional ceiling directory). Each file is read and parsed at most once
/// per resolver, and the chain of files is remembered for each directory, so a run over thousands of files does not
/// read anything twice. Matching a file's name against the sections is repeated for each file, because a section can
/// name one file. The resolver is safe to use from several threads.
/// </summary>
public sealed class EditorConfigResolver
{
    private const string FileName = ".editorconfig";

    private readonly Func<string, string?> readFile;
    private readonly string? ceiling;
    private readonly StringComparer comparer;
    private readonly Lock gate = new();
    private readonly Dictionary<string, ConfigChain> chains;

    /// <summary>Initializes a new instance of the <see cref="EditorConfigResolver"/> class.</summary>
    /// <param name="readFile">Returns the text of the file at a path, or <see langword="null"/> when there is no such file. It may throw an <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> for a file that cannot be read; the file is then skipped with a warning.</param>
    /// <param name="ceiling">A directory above which no <c>.editorconfig</c> is looked for, or <see langword="null"/> to search up to the root of the file system.</param>
    public EditorConfigResolver(Func<string, string?> readFile, string? ceiling = null)
    {
        ArgumentNullException.ThrowIfNull(readFile);
        this.readFile = readFile;
        this.ceiling = ceiling is null ? null : TrimSeparator(Path.GetFullPath(ceiling));
        comparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        chains = new Dictionary<string, ConfigChain>(comparer);
    }

    /// <summary>Resolves the settings for one file.</summary>
    /// <param name="filePath">The path of a source file; it need not exist.</param>
    /// <returns>The options and the warnings about the configuration that applies.</returns>
    public ResolvedSettings Resolve(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        string full = Path.GetFullPath(filePath);
        string directory = Path.GetDirectoryName(full) ?? full;

        ConfigChain chain;
        lock (gate)
        {
            chain = Chain(directory);
        }

        var warnings = new List<string>(chain.Unreadable);
        var assignments = new Dictionary<string, List<Assignment>>(StringComparer.Ordinal);
        for (int i = chain.Files.Count - 1; i >= 0; i--)
        {
            warnings.AddRange(chain.Files[i].Warnings);
            Collect(chain.Files[i], full, assignments);
        }

        FormatOptions options = SettingsMapper.Map(assignments, warnings);
        return new ResolvedSettings(options, warnings);
    }

    private static string TrimSeparator(string path) =>
        path.Length > 1 ? path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : path;

    private static void Collect(LoadedEditorConfig config, string fullPath, Dictionary<string, List<Assignment>> assignments)
    {
        string directory = TrimSeparator(Path.GetDirectoryName(config.FilePath)!);
        string relative = fullPath[directory.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (Path.DirectorySeparatorChar != '/')
        {
            relative = relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        IReadOnlyList<EditorConfigSection> sections = config.Parsed.Sections;
        for (int i = 0; i < sections.Count; i++)
        {
            if (config.Matches(i, relative))
            {
                AddPairs(sections[i], config.FilePath, assignments);
            }
        }
    }

    private static void AddPairs(EditorConfigSection section, string filePath, Dictionary<string, List<Assignment>> assignments)
    {
        foreach (EditorConfigPair pair in section.Pairs)
        {
            if (!SettingsMapper.SupportedKeys.Contains(pair.Key))
            {
                continue;
            }

            if (!assignments.TryGetValue(pair.Key, out List<Assignment>? list))
            {
                assignments[pair.Key] = list = [];
            }

            list.Add(new Assignment(pair.Value, filePath, pair.Line));
        }
    }

    private ConfigChain Chain(string directory)
    {
        if (chains.TryGetValue(directory, out ConfigChain? known))
        {
            return known;
        }

        string path = Path.Combine(directory, FileName);
        LoadedEditorConfig? here = Load(path, out string? unreadable);
        string? parent = Path.GetDirectoryName(directory);
        bool stop = (here?.Parsed.IsRoot ?? false) || parent is null || (ceiling is not null && comparer.Equals(TrimSeparator(directory), ceiling));
        ConfigChain above = stop ? new ConfigChain([], []) : Chain(parent!);

        var files = new List<LoadedEditorConfig>(above.Files.Count + 1);
        if (here is not null)
        {
            files.Add(here);
        }

        files.AddRange(above.Files);
        var skipped = new List<string>(above.Unreadable.Count + 1);
        if (unreadable is not null)
        {
            skipped.Add(unreadable);
        }

        skipped.AddRange(above.Unreadable);
        return chains[directory] = new ConfigChain(files, skipped);
    }

    private LoadedEditorConfig? Load(string path, out string? unreadable)
    {
        unreadable = null;
        try
        {
            string? text = readFile(path);
            return text is null ? null : new LoadedEditorConfig(path, EditorConfigParser.Parse(text));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or NotSupportedException or ArgumentException)
        {
            unreadable = $"{path}: the file could not be read ({ex.Message}); ignored";
            return null;
        }
    }
}
