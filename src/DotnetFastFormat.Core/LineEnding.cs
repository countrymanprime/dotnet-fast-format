namespace DotnetFastFormat.Core;

/// <summary>A line terminator the formatter can be told to write.</summary>
public enum LineEnding
{
    /// <summary>A line feed (<c>\n</c>).</summary>
    Lf,

    /// <summary>A carriage return and a line feed (<c>\r\n</c>).</summary>
    CrLf,
}
