using System.CommandLine;

namespace DotnetFastFormat.Cli;

/// <summary>The <c>dotnet fast-format</c> command line.</summary>
/// <remarks>
/// Exit codes: 0 success, 1 a file would change (<c>--check</c>, not yet implemented), 2 an error.
/// An error never modifies an input (the fail-safe invariant in <c>AGENTS.md</c>).
/// </remarks>
public static class CliApp
{
    /// <summary>Exit code for any error, including invalid arguments.</summary>
    public const int ErrorExitCode = 2;

    /// <summary>Runs the command line.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="output">Receives normal output, such as help text.</param>
    /// <param name="error">Receives error messages.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var paths = new Argument<string[]>("paths")
        {
            Description = "Files or directories to format.",
            Arity = ArgumentArity.ZeroOrMore,
        };

        // A string[] argument swallows unknown options as if they were paths, so reject them here.
        // After a "--" delimiter a path may legitimately start with a dash.
        paths.Validators.Add(result =>
        {
            if (args.Contains("--", StringComparer.Ordinal))
            {
                return;
            }

            foreach (string value in result.GetValueOrDefault<string[]>() ?? [])
            {
                if (value.StartsWith('-'))
                {
                    result.AddError($"Unrecognized option '{value}'.");
                }
            }
        });

        var root = new RootCommand("Fast, .editorconfig-aware C# formatter");
        root.Arguments.Add(paths);
        root.SetAction(_ =>
        {
            error.WriteLine("Formatting is not implemented yet.");
            return ErrorExitCode;
        });

        try
        {
            ParseResult parse = root.Parse(args);
            if (parse.Errors.Count > 0)
            {
                foreach (var parseError in parse.Errors)
                {
                    error.WriteLine(parseError.Message);
                }

                return ErrorExitCode;
            }

            return parse.Invoke(new InvocationConfiguration { Output = output, Error = error });
        }
        catch (Exception ex)
        {
            error.WriteLine($"Error: {ex.Message}");
            return ErrorExitCode;
        }
    }
}
