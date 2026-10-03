namespace DotnetFastFormat.Tests.Corpus;

/// <summary>One pinned repository in <c>corpus/corpus.json</c>.</summary>
/// <param name="Name">Directory name under the corpus cache, so it must be a safe path segment.</param>
/// <param name="Url">HTTPS GitHub URL of the repository.</param>
/// <param name="Tag">The release tag the pin was taken from. Informational: only <paramref name="Sha"/> is fetched.</param>
/// <param name="Sha">The full 40-character commit hash that is checked out.</param>
/// <param name="License">SPDX identifier of the repository's license.</param>
public sealed record CorpusRepository(string Name, string Url, string Tag, string Sha, string License);
