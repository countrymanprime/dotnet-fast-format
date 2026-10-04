using System.Globalization;

namespace DotnetFastFormat.Benchmarks;

/// <summary>Command-line options of the benchmark harness.</summary>
/// <param name="Runs">Runs per tool and repository.</param>
/// <param name="Repositories">Repository names to measure; empty means all.</param>
/// <param name="Tools">Tool names to measure; empty means all.</param>
/// <param name="OutputDirectory">Where to write the report files, or null to only print.</param>
internal sealed record BenchmarkOptions(int Runs, IReadOnlySet<string> Repositories, IReadOnlySet<string> Tools, string? OutputDirectory)
{
    /// <summary>The usage text.</summary>
    public const string Usage = "Usage: DotnetFastFormat.Benchmarks [--runs N] [--repo NAME]... [--tool NAME]... [--output DIR]\n"
        + "Tools: dotnet-format-whitespace, dotnet-format, csharpier";

    /// <summary>Parses the command line.</summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The options, or an error message when the arguments are invalid.</returns>
    public static (BenchmarkOptions? Options, string? Error) Parse(IReadOnlyList<string> args)
    {
        int runs = 3;
        var repositories = new HashSet<string>(StringComparer.Ordinal);
        var tools = new HashSet<string>(StringComparer.Ordinal);
        string? output = null;

        for (int i = 0; i < args.Count; i++)
        {
            string name = args[i];
            if (i + 1 >= args.Count)
            {
                return (null, $"Option '{name}' needs a value.");
            }

            string value = args[++i];
            switch (name)
            {
                case "--runs":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out runs) || runs < 1)
                    {
                        return (null, $"--runs needs a whole number of at least 1, not '{value}'.");
                    }

                    break;
                case "--repo":
                    repositories.Add(value);
                    break;
                case "--tool":
                    if (!ToolNames.All.Contains(value, StringComparer.Ordinal))
                    {
                        return (null, $"Unknown tool '{value}'. Known tools: {string.Join(", ", ToolNames.All)}.");
                    }

                    tools.Add(value);
                    break;
                case "--output":
                    output = value;
                    break;
                default:
                    return (null, $"Unknown option '{name}'.");
            }
        }

        return (new BenchmarkOptions(runs, repositories, tools, output), null);
    }
}
