using System.Runtime.InteropServices;
using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Benchmarks;

/// <summary>Times each tool on each corpus repository.</summary>
internal static class BenchmarkRunner
{
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromMinutes(30);

    /// <summary>Fetches the pinned corpus if needed, runs every selected tool on every selected repository, and reports.</summary>
    /// <param name="options">What to measure.</param>
    /// <param name="progress">Where progress lines go.</param>
    /// <returns>The measurements.</returns>
    /// <exception cref="ArgumentException">A requested repository is not in the corpus manifest.</exception>
    public static BenchmarkReport Run(BenchmarkOptions options, TextWriter progress)
    {
        IReadOnlyList<CorpusRepository> manifest = CorpusManifest.Load(CorpusManifest.DefaultPath());
        string[] unknown = [.. options.Repositories.Where(name => !manifest.Any(r => string.Equals(r.Name, name, StringComparison.Ordinal)))];
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown repository: {string.Join(", ", unknown)}. Known: {string.Join(", ", manifest.Select(r => r.Name))}.", nameof(options));
        }

        CorpusRepository[] repositories = [.. manifest.Where(r => options.Repositories.Count == 0 || options.Repositories.Contains(r.Name))];
        string root = RepositoryRoot.Find();
        string workspace = Path.Combine(root, ".bench");
        string[] toolNames = [.. ToolNames.All.Where(name => options.Tools.Count == 0 || options.Tools.Contains(name))];
        IBenchmarkTool[] tools = [.. toolNames.Select(name => Create(name, Path.Combine(workspace, "tools")))];

        var infos = new List<RepositoryInfo>();
        var results = new List<ToolResult>();
        foreach (CorpusRepository repository in repositories)
        {
            progress.WriteLine($"{repository.Name}: fetching pinned commit {repository.Sha[..10]}");
            string checkout = CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot()).Path;
            infos.Add(Describe(repository.Name, checkout));

            foreach (IBenchmarkTool tool in tools)
            {
                var runs = new List<Measurement>();
                for (int run = 1; run <= options.Runs; run++)
                {
                    string workingCopy = Path.Combine(workspace, "work", repository.Name + "-" + tool.Name);
                    Measurement? measurement = MeasureOnce(tool, checkout, workingCopy);
                    if (measurement is null)
                    {
                        progress.WriteLine($"{repository.Name} {tool.Name}: not applicable");
                        break;
                    }

                    progress.WriteLine($"{repository.Name} {tool.Name} run {run}/{options.Runs}: "
                        + (measurement.Succeeded ? $"{measurement.Seconds:0.0} s, {measurement.ChangedFiles} files changed" : $"failed ({measurement.Error})"));
                    runs.Add(measurement);
                }

                if (runs.Count > 0)
                {
                    results.Add(new ToolResult(repository.Name, tool.Name, runs));
                }
            }
        }

        var environment = new EnvironmentInfo(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount,
            ProcessRunner.Capture("dotnet", root, "--version"),
            DescribeCommit(root),
            tools.ToDictionary(tool => tool.Name, tool => tool.Version(), StringComparer.Ordinal));
        return new BenchmarkReport(environment, options.Runs, infos, results);
    }

    private static string DescribeCommit(string root)
    {
        string commit = GitRunner.RunChecked(root, "rev-parse", "HEAD").Trim();
        bool dirty = !string.IsNullOrWhiteSpace(GitRunner.RunChecked(root, "status", "--porcelain", "--untracked-files=no"));
        return dirty ? commit + " (plus uncommitted changes)" : commit;
    }

    private static IBenchmarkTool Create(string name, string toolsDirectory) => name switch
    {
        ToolNames.DotnetFormatWhitespace => new DotnetFormatWhitespaceTool(),
        ToolNames.DotnetFormat => new DotnetFormatTool(),
        ToolNames.CSharpier => new CSharpierTool(toolsDirectory),
        _ => throw new ArgumentException($"Unknown tool '{name}'.", nameof(name)),
    };

    private static Measurement? MeasureOnce(IBenchmarkTool tool, string checkout, string workingCopy)
    {
        DirectoryCleanup.Delete(workingCopy);
        CopyDirectory(checkout, workingCopy);
        try
        {
            ProcessSpec? command = tool.Command(workingCopy);
            if (command is null)
            {
                return null;
            }

            if (tool.Prepare(workingCopy) is { } prepare)
            {
                ProcessOutcome prepared = ProcessRunner.Run(prepare, ToolTimeout);
                if (prepared.ExitCode != 0)
                {
                    return new Measurement(false, 0, 0, $"prepare failed, exit code {prepared.ExitCode}: {prepared.OutputTail}");
                }
            }

            ProcessOutcome outcome = ProcessRunner.Run(command, ToolTimeout);
            if (outcome.ExitCode != 0)
            {
                return new Measurement(false, outcome.Seconds, 0, $"exit code {outcome.ExitCode}: {outcome.OutputTail}");
            }

            // Untracked files (bin, obj) are build output, not formatting work.
            string status = GitRunner.RunChecked(workingCopy, "status", "--porcelain", "--untracked-files=no");
            int changed = status.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            return new Measurement(true, outcome.Seconds, changed, null);
        }
        finally
        {
            DirectoryCleanup.Delete(workingCopy);
        }
    }

    private static RepositoryInfo Describe(string name, string checkout)
    {
        FileInfo[] files = [.. new DirectoryInfo(checkout).EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Where(file => !file.FullName.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar, StringComparison.Ordinal))];
        return new RepositoryInfo(name, files.Length, files.Sum(file => file.Length) / (1024.0 * 1024.0));
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }
    }
}
