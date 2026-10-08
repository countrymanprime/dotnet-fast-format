namespace DotnetFastFormat.Core;

/// <summary>
/// The settings that change what the formatter prints (ADR 0007, ADR 0012). The defaults are the style used when no
/// <c>.editorconfig</c> key applies. A value outside its documented range is rejected when it is set.
/// </summary>
public sealed record FormatOptions
{
    /// <summary>The largest indent size and tab width accepted.</summary>
    public const int MaxColumns = 256;

    /// <summary>The largest line length accepted; use <see langword="null"/> for no limit.</summary>
    public const int MaxLineLengthLimit = 1_000_000;

    private const int DefaultIndentSize = 4;
    private const int DefaultLineLength = 100;

    private readonly int indentSize = DefaultIndentSize;
    private readonly int tabWidth = DefaultIndentSize;
    private readonly int? maxLineLength = DefaultLineLength;

    /// <summary>Gets the options used when nothing is configured: four spaces, 100 columns, the dominant line ending, one final newline, the file's own byte order mark.</summary>
    public static FormatOptions Default { get; } = new();

    /// <summary>Gets how one indentation level is written.</summary>
    public IndentStyle IndentStyle { get; init; } = IndentStyle.Space;

    /// <summary>Gets the columns of one indentation level, from 1 to <see cref="MaxColumns"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside that range.</exception>
    public int IndentSize
    {
        get => indentSize;
        init => indentSize = CheckColumns(value);
    }

    /// <summary>Gets the columns one tab character counts for, from 1 to <see cref="MaxColumns"/>. It matters only for <see cref="IndentStyle.Tab"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside that range.</exception>
    public int TabWidth
    {
        get => tabWidth;
        init => tabWidth = CheckColumns(value);
    }

    /// <summary>Gets the width at which lines wrap, from 1 to <see cref="MaxLineLengthLimit"/>, or <see langword="null"/> for never.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside that range.</exception>
    public int? MaxLineLength
    {
        get => maxLineLength;
        init => maxLineLength = value is { } length ? CheckLineLength(length) : null;
    }

    /// <summary>Gets the line terminator to write, or <see langword="null"/> for the one that is most common in the input (LF when equal).</summary>
    public LineEnding? EndOfLine { get; init; }

    /// <summary>Gets a value indicating whether a non-empty file ends with a line terminator.</summary>
    public bool InsertFinalNewline { get; init; } = true;

    /// <summary>Gets what the caller does with a UTF-8 byte order mark. The formatter itself works on text and does not use it.</summary>
    public ByteOrderMarkPolicy ByteOrderMark { get; init; } = ByteOrderMarkPolicy.Preserve;

    private static int CheckLineLength(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxLineLengthLimit);
        return value;
    }

    private static int CheckColumns(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxColumns);
        return value;
    }
}
