namespace DotnetFastFormat.Benchmarks;

/// <summary>A formatter the harness can time.</summary>
internal interface IBenchmarkTool
{
    /// <summary>The tool name, one of <see cref="ToolNames"/>.</summary>
    string Name { get; }

    /// <summary>The version to record in the report.</summary>
    string Version();

    /// <summary>An untimed command to run first in the working copy, such as a restore, or null.</summary>
    ProcessSpec? Prepare(string workingCopy);

    /// <summary>The timed command, or null when the tool cannot run on this working copy.</summary>
    ProcessSpec? Command(string workingCopy);

    /// <summary>Whether a run counts as having formatted the repository. The default is exit code 0.</summary>
    bool IsSuccess(ProcessOutcome outcome) => outcome.ExitCode == 0;
}
