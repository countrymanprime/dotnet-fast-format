namespace DotnetFastFormat.Cli;

/// <summary>What happened to one file.</summary>
internal enum FileOutcome
{
    /// <summary>The file was already formatted and was not written.</summary>
    Unchanged,

    /// <summary>The file was rewritten.</summary>
    Formatted,

    /// <summary>The file was left untouched because of an error.</summary>
    Failed,
}
