namespace DotnetFastFormat.Tests.Corpus;

/// <summary>Checks out each pinned corpus repository at its exact commit.</summary>
public static class CorpusFetcher
{
    /// <summary>The directory corpus checkouts live in: <c>DOTNET_FAST_FORMAT_CORPUS_DIR</c> or <c>.corpus</c> at the repository root.</summary>
    /// <returns>The absolute path of the corpus cache.</returns>
    public static string CorpusRoot() =>
        Environment.GetEnvironmentVariable("DOTNET_FAST_FORMAT_CORPUS_DIR") is { Length: > 0 } custom
            ? Path.GetFullPath(custom)
            : Path.Combine(RepositoryRoot.Find(), ".corpus");

    /// <summary>
    /// Makes <c>&lt;corpusRoot&gt;/&lt;name&gt;</c> a clean checkout of the pinned commit.
    /// An existing clean checkout of that commit is reused; a modified or stale one is replaced.
    /// </summary>
    /// <param name="repository">The pinned repository.</param>
    /// <param name="corpusRoot">The directory that holds all checkouts.</param>
    /// <returns>Where the checkout is and whether it was reused.</returns>
    /// <exception cref="InvalidOperationException">
    /// The name escapes <paramref name="corpusRoot"/>, the target holds something that is not a git checkout,
    /// or git could not fetch exactly the pinned commit.
    /// </exception>
    public static FetchResult EnsureFetched(CorpusRepository repository, string corpusRoot)
    {
        string root = Path.GetFullPath(corpusRoot);
        string target = Path.GetFullPath(Path.Combine(root, repository.Name));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Corpus name '{repository.Name}' would place the checkout outside '{root}'.");
        }

        if (Directory.Exists(target))
        {
            if (IsCleanCheckoutOf(target, repository.Sha))
            {
                return new FetchResult(target, Reused: true);
            }

            if (!Directory.Exists(Path.Combine(target, ".git")))
            {
                throw new InvalidOperationException($"'{target}' exists but is not a git checkout; refusing to delete it.");
            }

            TestDirectory.Delete(target);
        }

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(target);
        GitRunner.RunChecked(target, "init", "-q");

        // Identical bytes on every platform: no line-ending conversion, and long Windows paths allowed.
        GitRunner.RunChecked(target, "config", "core.autocrlf", "false");
        GitRunner.RunChecked(target, "config", "core.longpaths", "true");
        GitRunner.RunChecked(target, "remote", "add", "origin", repository.Url);
        GitRunner.RunChecked(target, "fetch", "-q", "--depth", "1", "origin", repository.Sha);
        GitRunner.RunChecked(target, "checkout", "-q", "--detach", "FETCH_HEAD");

        string head = GitRunner.RunChecked(target, "rev-parse", "HEAD").Trim();
        return string.Equals(head, repository.Sha, StringComparison.Ordinal)
            ? new FetchResult(target, Reused: false)
            : throw new InvalidOperationException($"'{repository.Name}' checked out {head}, expected the pinned {repository.Sha}.");
    }

    private static bool IsCleanCheckoutOf(string directory, string sha)
    {
        if (!Directory.Exists(Path.Combine(directory, ".git")))
        {
            return false;
        }

        (int headExit, string head, _) = GitRunner.Run(directory, "rev-parse", "HEAD");
        if (headExit != 0 || !string.Equals(head.Trim(), sha, StringComparison.Ordinal))
        {
            return false;
        }

        (int statusExit, string status, _) = GitRunner.Run(directory, "status", "--porcelain");
        return statusExit == 0 && string.IsNullOrWhiteSpace(status);
    }
}
