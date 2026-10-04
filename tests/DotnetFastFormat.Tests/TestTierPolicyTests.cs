using System.Reflection;
using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests;

/// <summary>Keeps the test tiers honest (ADR 0005): a tier nothing runs would be a way to skip tests.</summary>
public class TestTierPolicyTests
{
    private static readonly string[] KnownCategories = ["Slow"];

    [Fact]
    public void OnlyKnownCategoriesAreUsed()
    {
        IEnumerable<string> used = typeof(TestTierPolicyTests).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<TraitAttribute>(inherit: true)
                .Concat(type.GetMethods().SelectMany(method => method.GetCustomAttributes<TraitAttribute>(inherit: true))))
            .Where(trait => string.Equals(trait.Name, "Category", StringComparison.Ordinal))
            .Select(trait => trait.Value)
            .Distinct(StringComparer.Ordinal);

        Assert.Empty(used.Except(KnownCategories, StringComparer.Ordinal));
    }

    [Fact]
    public void TheSlowTierHasAScheduledJobThatRunsIt()
    {
        string workflow = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), ".github", "workflows", "slow.yml"));

        Assert.Contains("schedule:", workflow, StringComparison.Ordinal);
        Assert.Contains("-p:TestTier=slow", workflow, StringComparison.Ordinal);
    }
}
