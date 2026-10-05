using System.CommandLine;
using DotnetFastFormat.Core;
using DotnetFastFormat.Core.Config;

namespace DotnetFastFormat.Cli;

/// <summary>The <c>dotnet fast-format</c> command line.</summary>
/// <remarks>
/// Exit codes: 0 success, 1 a file would change (<c>--check</c>, not yet implemented), 2 an error.
/// A file that fails is never modified, and the other files are still processed (requirements CLI-001 and CLI-003 in <c>docs/requirements/core.md</c>).
/// A warning about an <c>.editorconfig</c> (an invalid value, an unreadable file) goes to the error stream once and does not change the exit code (CFG-010, CFG-011).
/// </remarks>
public static class CliApp
{
    /// <summary>Exit code for any error, including invalid arguments.</summary>
    public const int ErrorExitCode = 2;

    private const string Description =
        "Fast, .editorconfig-aware C# formatter. Formats files in place. Settings come from the .editorconfig files that "
        + "apply to each file, nearest first up to root = true: indent_style, indent_size, tab_width, max_line_length, "
        + "end_of_line (lf or crlf), insert_final_newline and charset (utf-8 or utf-8-bom). Other keys are ignored; "
        + "an invalid value is reported on stderr and ignored. Without a key: 4 spaces, 100 columns, the file's line "
        + "ending and BOM, one final newline.";

    /// <summary>Runs the command line.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="output">Receives normal output, such as help text.</param>
    /// <param name="error">Receives error messages.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args, TextWriter output, TextWriter error) =>
        Run(args, output, error, new RoslynFormatter(), new DiskFileStore());

    /// <summary>Runs the command line with the given formatter and file store.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="output">Receives normal output, such as help text.</param>
    /// <param name="error">Receives error messages.</param>
    /// <param name="formatter">The formatter to apply.</param>
    /// <param name="store">Where files are read and written.</param>
    /// <returns>The process exit code.</returns>
    internal static int Run(string[] args, TextWriter output, TextWriter error, IFormatter formatter, IFileStore store) =>
        Run(args, output, error, formatter, store, DiskConfigReader.Read);

    /// <summary>Runs the command line with the given formatter, file store and way of reading <c>.editorconfig</c> files.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="output">Receives normal output, such as help text.</param>
    /// <param name="error">Receives error messages and warnings.</param>
    /// <param name="formatter">The formatter to apply.</param>
    /// <param name="store">Where files are read and written.</param>
    /// <param name="readConfig">Returns the text of an <c>.editorconfig</c> at a path, or <see langword="null"/> when there is none.</param>
    /// <returns>The process exit code.</returns>
    internal static int Run(string[] args, TextWriter output, TextWriter error, IFormatter formatter, IFileStore store, Func<string, string?> readConfig)
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

        var root = new RootCommand(Description);
        root.Arguments.Add(paths);
        root.SetAction(parse => FormatFiles(parse.GetValue(paths) ?? [], formatter, store, readConfig, output, error));

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

    private static int FormatFiles(string[] paths, IFormatter formatter, IFileStore store, Func<string, string?> readConfig, TextWriter output, TextWriter error)
    {
        if (paths.Length == 0)
        {
            error.WriteLine("No paths given. Pass files or directories to format, or --help.");
            return ErrorExitCode;
        }

        var warned = new HashSet<string>(StringComparer.Ordinal);
        var processor = new FileProcessor(
            formatter,
            store,
            new EditorConfigResolver(readConfig),
            message =>
            {
                if (warned.Add(message))
                {
                    error.WriteLine($"warning: {message}");
                }
            });
        var errors = new List<string>();
        List<string> files = PathExpander.Expand(paths, errors);
        int formatted = 0;
        int unchanged = 0;
        foreach (string file in files)
        {
            (FileOutcome outcome, string message) = processor.Process(file);
            switch (outcome)
            {
                case FileOutcome.Formatted:
                    formatted++;
                    break;
                case FileOutcome.Unchanged:
                    unchanged++;
                    break;
                default:
                    errors.Add($"{file}: {message}");
                    break;
            }
        }

        foreach (string message in errors)
        {
            error.WriteLine(message);
        }

        output.WriteLine($"Formatted {formatted} file(s), {unchanged} unchanged, {errors.Count} failed.");
        return errors.Count == 0 ? 0 : ErrorExitCode;
    }
}
