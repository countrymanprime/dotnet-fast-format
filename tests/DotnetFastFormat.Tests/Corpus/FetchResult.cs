namespace DotnetFastFormat.Tests.Corpus;

/// <summary>The outcome of fetching one corpus repository.</summary>
/// <param name="Path">The checkout directory.</param>
/// <param name="Reused">True when an existing checkout already matched the pin and nothing was fetched.</param>
public sealed record FetchResult(string Path, bool Reused);
