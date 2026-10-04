using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DotnetFastFormat.Corpus;

/// <summary>Loads and validates <c>corpus/corpus.json</c>, the list of pinned corpus repositories.</summary>
public static partial class CorpusManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>The manifest in the repository checkout.</summary>
    /// <returns>The absolute path of <c>corpus/corpus.json</c>.</returns>
    public static string DefaultPath() => Path.Combine(RepositoryRoot.Find(), "corpus", "corpus.json");

    /// <summary>Reads a manifest file.</summary>
    /// <param name="path">Path of the JSON file.</param>
    /// <returns>The repositories it lists.</returns>
    public static IReadOnlyList<CorpusRepository> Load(string path) => Parse(File.ReadAllText(path));

    /// <summary>Parses manifest JSON. Unknown properties are an error, so a typo cannot silently drop a field.</summary>
    /// <param name="json">The manifest text.</param>
    /// <returns>The repositories it lists.</returns>
    public static IReadOnlyList<CorpusRepository> Parse(string json) =>
        JsonSerializer.Deserialize<ManifestFile>(json, JsonOptions)?.Repositories
        ?? throw new JsonException("The corpus manifest is empty.");

    /// <summary>Checks every entry for the properties the fetcher relies on.</summary>
    /// <param name="repositories">The entries to check.</param>
    /// <returns>One message per problem; empty when the manifest is valid.</returns>
    public static IReadOnlyList<string> Validate(IReadOnlyList<CorpusRepository> repositories)
    {
        var errors = new List<string>();
        if (repositories.Count == 0)
        {
            errors.Add("The manifest lists no repositories.");
        }

        foreach (CorpusRepository repository in repositories)
        {
            if (!NamePattern().IsMatch(repository.Name))
            {
                errors.Add($"'{repository.Name}': name must be lowercase letters, digits, '.', '-' or '_', starting with a letter or digit (it becomes a directory name).");
            }

            if (!UrlPattern().IsMatch(repository.Url))
            {
                errors.Add($"'{repository.Name}': url must look like https://github.com/<owner>/<repo>.");
            }

            if (!ShaPattern().IsMatch(repository.Sha))
            {
                errors.Add($"'{repository.Name}': sha must be a full 40-character lowercase commit hash, not a branch or tag.");
            }

            if (string.IsNullOrWhiteSpace(repository.License))
            {
                errors.Add($"'{repository.Name}': license (an SPDX identifier) is required.");
            }
        }

        foreach (IGrouping<string, CorpusRepository> group in repositories.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1)
            {
                errors.Add($"'{group.Key}': duplicate name.");
            }
        }

        return errors;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"^https://github\.com/[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UrlPattern();

    [GeneratedRegex("^[0-9a-f]{40}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ShaPattern();

    private sealed record ManifestFile(IReadOnlyList<CorpusRepository> Repositories);
}
