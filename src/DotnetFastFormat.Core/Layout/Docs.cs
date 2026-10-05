namespace DotnetFastFormat.Core.Layout;

/// <summary>Builders for the documents the printer lays out.</summary>
internal static class Docs
{
    /// <summary>Gets a document that prints nothing.</summary>
    public static Doc Empty { get; } = new ConcatDoc([]);

    /// <summary>Gets a line that is a space when flat and a line break when broken.</summary>
    public static Doc Line { get; } = new LineDoc(LineKind.Normal);

    /// <summary>Gets a line that is nothing when flat and a line break when broken.</summary>
    public static Doc SoftLine { get; } = new LineDoc(LineKind.Soft);

    /// <summary>Gets a line break that is always taken; every enclosing group breaks.</summary>
    public static Doc HardLine { get; } = new LineDoc(LineKind.Hard);

    /// <summary>Creates single-line text.</summary>
    /// <param name="value">The text, which must not contain a line break.</param>
    /// <returns>The document.</returns>
    public static Doc Text(string value) => new TextDoc(value);

    /// <summary>Creates source text that is emitted exactly as written.</summary>
    /// <param name="value">The text, which may contain line breaks.</param>
    /// <returns>The document.</returns>
    public static Doc Verbatim(string value) => new VerbatimDoc(value);

    /// <summary>Creates a document that prints its parts in order.</summary>
    /// <param name="parts">The parts.</param>
    /// <returns>The document.</returns>
    public static Doc Concat(params Doc[] parts) => new ConcatDoc(parts);

    /// <summary>Creates a document that prints its parts in order.</summary>
    /// <param name="parts">The parts.</param>
    /// <returns>The document.</returns>
    public static Doc Concat(IEnumerable<Doc> parts) => new ConcatDoc([.. parts]);

    /// <summary>Creates a group that is flat when it fits and broken otherwise.</summary>
    /// <param name="contents">The grouped document.</param>
    /// <returns>The document.</returns>
    public static Doc Group(Doc contents) => new GroupDoc(contents);

    /// <summary>Creates a document whose line breaks are indented one level deeper.</summary>
    /// <param name="contents">The indented document.</param>
    /// <returns>The document.</returns>
    public static Doc Indent(Doc contents) => new IndentDoc(contents);

    /// <summary>Creates a document that depends on whether its enclosing group is broken.</summary>
    /// <param name="breakContents">Printed when the enclosing group is broken.</param>
    /// <param name="flatContents">Printed when the enclosing group is flat.</param>
    /// <returns>The document.</returns>
    public static Doc IfBreak(Doc breakContents, Doc flatContents) => new IfBreakDoc(breakContents, flatContents);

    /// <summary>Creates a fill from alternating items and separators.</summary>
    /// <param name="parts">Items and separators, starting and ending with an item.</param>
    /// <returns>The document.</returns>
    public static Doc Fill(IEnumerable<Doc> parts) => new FillDoc([.. parts]);

    /// <summary>Creates a document that puts a separator between items.</summary>
    /// <param name="separator">The separator, printed between every pair of items.</param>
    /// <param name="items">The items.</param>
    /// <returns>The document.</returns>
    public static Doc Join(Doc separator, IEnumerable<Doc> items)
    {
        var parts = new List<Doc>();
        foreach (Doc item in items)
        {
            if (parts.Count > 0)
            {
                parts.Add(separator);
            }

            parts.Add(item);
        }

        return new ConcatDoc(parts);
    }
}
