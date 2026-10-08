using System.Globalization;

namespace DotnetFastFormat.Core.Config;

/// <summary>
/// Reads the values of the supported keys (ADR 0012). Each method returns <see langword="null"/> when the value is
/// usable, and otherwise the end of a warning sentence, such as <c>is not space or tab</c>. Values are
/// case-insensitive. A number is written with digits only.
/// </summary>
internal static class SettingValues
{
    /// <summary>The <c>indent_size</c> value that stands for <c>tab</c>; no number can be 0.</summary>
    public const int Tab = 0;

    /// <summary>The <c>max_line_length</c> value that stands for <c>off</c>; no number can be 0.</summary>
    public const int Off = 0;

    /// <summary>Reads <c>indent_style</c>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="style">The style when usable.</param>
    /// <returns>The reason it cannot be used, or <see langword="null"/>.</returns>
    public static string? ParseIndentStyle(string value, out IndentStyle style)
    {
        style = IndentStyle.Space;
        if (value.Equals("space", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        style = IndentStyle.Tab;
        return value.Equals("tab", StringComparison.OrdinalIgnoreCase) ? null : "is not space or tab";
    }

    /// <summary>Reads <c>indent_size</c>: a number of columns, or <c>tab</c> (<see cref="Tab"/>).</summary>
    /// <param name="value">The value.</param>
    /// <param name="size">The columns, or <see cref="Tab"/>.</param>
    /// <returns>The reason it cannot be used, or <see langword="null"/>.</returns>
    public static string? ParseIndentSize(string value, out int size)
    {
        if (value.Equals("tab", StringComparison.OrdinalIgnoreCase))
        {
            size = Tab;
            return null;
        }

        return Number(value, FormatOptions.MaxColumns, out size) ? null : $"is not a whole number from 1 to {FormatOptions.MaxColumns}, or tab";
    }

    /// <summary>Reads <c>tab_width</c>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="width">The columns when usable.</param>
    /// <returns>The reason it cannot be used, or <see langword="null"/>.</returns>
    public static string? ParseTabWidth(string value, out int width) =>
        Number(value, FormatOptions.MaxColumns, out width) ? null : $"is not a whole number from 1 to {FormatOptions.MaxColumns}";

    /// <summary>Reads <c>max_line_length</c>: a number, or <c>off</c> (<see cref="Off"/>).</summary>
    /// <param name="value">The value.</param>
    /// <param name="length">The width, or <see cref="Off"/>.</param>
    /// <returns>The reason it cannot be used, or <see langword="null"/>.</returns>
    public static string? ParseMaxLineLength(string value, out int length)
    {
        if (value.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            length = Off;
            return null;
        }

        return Number(value, FormatOptions.MaxLineLengthLimit, out length) ? null : $"is not a whole number from 1 to {FormatOptions.MaxLineLengthLimit}, or off";
    }

    /// <summary>Reads <c>end_of_line</c>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="ending">The terminator when usable.</param>
    /// <returns>The reason it cannot be used, or <see langword="null"/>.</returns>
    public static string? ParseEndOfLine(string value, out LineEnding ending)
    {
        ending = LineEnding.Lf;
        if (value.Equals("lf", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (value.Equals("crlf", StringComparison.OrdinalIgnoreCase))
        {
            ending = LineEnding.CrLf;
            return null;
        }

        return value.Equals("cr", StringComparison.OrdinalIgnoreCase) ? "is not supported (use lf or crlf)" : "is not lf or crlf";
    }

    /// <summary>Reads <c>insert_final_newline</c>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="insert">The setting when usable.</param>
    /// <returns>The reason it cannot be used, or <see langword="null"/>.</returns>
    public static string? ParseFinalNewline(string value, out bool insert)
    {
        insert = value.Equals("true", StringComparison.OrdinalIgnoreCase);
        return insert || value.Equals("false", StringComparison.OrdinalIgnoreCase) ? null : "is not true or false";
    }

    /// <summary>Reads <c>charset</c>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="policy">What to do with the byte order mark when usable.</param>
    /// <returns>The reason it cannot be used, or <see langword="null"/>.</returns>
    public static string? ParseCharset(string value, out ByteOrderMarkPolicy policy)
    {
        policy = ByteOrderMarkPolicy.Remove;
        if (value.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        policy = ByteOrderMarkPolicy.Add;
        if (value.Equals("utf-8-bom", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value.Equals("latin1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("utf-16be", StringComparison.OrdinalIgnoreCase)
            || value.Equals("utf-16le", StringComparison.OrdinalIgnoreCase)
            ? "is not supported (files are read and written as UTF-8)"
            : "is not utf-8 or utf-8-bom";
    }

    private static bool Number(string value, int max, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number >= 1 && number <= max;
}
