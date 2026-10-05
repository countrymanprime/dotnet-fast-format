namespace DotnetFastFormat.Core.Config;

/// <summary>
/// Turns the assignments that apply to a file into <see cref="FormatOptions"/> (ADR 0012). For each key the last
/// assignment that is usable wins; <c>unset</c> ends the key's history; an assignment that is not usable is reported
/// and skipped, so an earlier one still applies.
/// </summary>
internal static class SettingsMapper
{
    private const string Unset = "unset";

    private delegate string? Parser<T>(string value, out T result);

    /// <summary>Gets the keys the formatter reads; every other key is ignored.</summary>
    public static IReadOnlySet<string> SupportedKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "indent_style",
        "indent_size",
        "tab_width",
        "max_line_length",
        "end_of_line",
        "insert_final_newline",
        "charset",
    };

    /// <summary>Maps the assignments of one file.</summary>
    /// <param name="assignments">For each supported key, every assignment that applies, farthest and earliest first.</param>
    /// <param name="warnings">Receives a message for each assignment that is not usable.</param>
    /// <returns>The options.</returns>
    public static FormatOptions Map(Dictionary<string, List<Assignment>> assignments, List<string> warnings)
    {
        bool hasStyle = Pick(assignments, "indent_style", SettingValues.ParseIndentStyle, warnings, out IndentStyle style);
        bool hasSize = Pick(assignments, "indent_size", SettingValues.ParseIndentSize, warnings, out int size);
        bool hasTabWidth = Pick(assignments, "tab_width", SettingValues.ParseTabWidth, warnings, out int tabWidth);
        bool hasLength = Pick(assignments, "max_line_length", SettingValues.ParseMaxLineLength, warnings, out int length);
        bool hasEnding = Pick(assignments, "end_of_line", SettingValues.ParseEndOfLine, warnings, out LineEnding ending);
        bool hasFinal = Pick(assignments, "insert_final_newline", SettingValues.ParseFinalNewline, warnings, out bool insertFinal);
        bool hasCharset = Pick(assignments, "charset", SettingValues.ParseCharset, warnings, out ByteOrderMarkPolicy bom);

        // The specification's rules: with tabs and no indent_size the size is "tab"; "tab" means tab_width; a tab is
        // as wide as the indent size unless tab_width says otherwise. Anything still open takes the default.
        if (!hasSize && hasStyle && style == IndentStyle.Tab)
        {
            hasSize = true;
            size = SettingValues.Tab;
        }

        int indentSize = !hasSize ? FormatOptions.Default.IndentSize : size == SettingValues.Tab ? (hasTabWidth ? tabWidth : FormatOptions.Default.IndentSize) : size;
        int width = hasTabWidth ? tabWidth : indentSize;

        return new FormatOptions
        {
            IndentStyle = hasStyle ? style : IndentStyle.Space,
            IndentSize = indentSize,
            TabWidth = width,
            MaxLineLength = !hasLength ? FormatOptions.Default.MaxLineLength : length == SettingValues.Off ? null : length,
            EndOfLine = hasEnding ? ending : null,
            InsertFinalNewline = !hasFinal || insertFinal,
            ByteOrderMark = hasCharset ? bom : ByteOrderMarkPolicy.Preserve,
        };
    }

    private static bool Pick<T>(Dictionary<string, List<Assignment>> assignments, string key, Parser<T> parse, List<string> warnings, out T value)
    {
        value = default!;
        if (!assignments.TryGetValue(key, out List<Assignment>? list))
        {
            return false;
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            Assignment assignment = list[i];
            if (assignment.Value.Equals(Unset, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string? problem = parse(assignment.Value, out value);
            if (problem is null)
            {
                return true;
            }

            warnings.Add($"{assignment.Path}({assignment.Line}): {key} = {assignment.Value} {problem}; ignored");
        }

        value = default!;
        return false;
    }
}
