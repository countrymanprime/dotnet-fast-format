using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests;

/// <summary>Keeps the pull-request and post-merge workflows running the same checks.</summary>
public class WorkflowPolicyTests
{
    private const string SharedChecks = "uses: ./.github/workflows/checks.yml";

    [Fact]
    public void PullRequestsRunTheSharedChecks()
    {
        string workflow = Read("pull-request.yml");

        Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
        Assert.Contains(SharedChecks, workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainRunsTheSameSharedChecksAfterMerge()
    {
        string workflow = Read("main.yml");

        Assert.Contains("push:", workflow, StringComparison.Ordinal);
        Assert.Contains("branches: [main]", workflow, StringComparison.Ordinal);
        Assert.Contains(SharedChecks, workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSharedChecksRunTheGateAndTheStyleCheck()
    {
        string workflow = Read("checks.yml");

        Assert.Contains("workflow_call:", workflow, StringComparison.Ordinal);
        Assert.Contains("run: dotnet test", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet format DotnetFastFormat.slnx --verify-no-changes", workflow, StringComparison.Ordinal);
        Assert.Contains("windows-latest", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePullRequestWorkflowDoesNotRunTheBenchmarks()
    {
        // A path-filtered pull_request trigger applies to the whole PR diff, so it would rerun on every push.
        string workflow = Read("benchmarks.yml");

        Assert.DoesNotContain("pull_request:", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePostMergeWorkflowsRunTheSlowTierAndTheBenchmarks()
    {
        Assert.Contains("branches: [main]", Read("slow.yml"), StringComparison.Ordinal);
        Assert.Contains("branches: [main]", Read("benchmarks.yml"), StringComparison.Ordinal);
    }

    private static string Read(string name) =>
        File.ReadAllText(Path.Combine(RepositoryRoot.Find(), ".github", "workflows", name));
}
