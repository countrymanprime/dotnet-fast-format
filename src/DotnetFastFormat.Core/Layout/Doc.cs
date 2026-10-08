namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// A node of the document tree the printer lays out: text, groups, indents and line breaks. Documents are
/// immutable and built bottom-up, so whether a document forces its enclosing groups to break is known as soon
/// as it is constructed, without walking the tree afterwards.
/// </summary>
internal abstract class Doc
{
    /// <summary>
    /// Gets a value indicating whether this document contains a line break that is always taken (a hard line or
    /// multi-line verbatim text). Every enclosing group must then break. A conditional group does not pass this
    /// on, because it can choose a layout in which the break is the only one (see <see cref="ConditionalGroupDoc"/>).
    /// </summary>
    public bool ForcesBreak { get; protected init; }

    /// <summary>
    /// Gets a value indicating whether this document contains a line break that is always taken anywhere inside it,
    /// including inside a conditional group. Printers use it to decide between layouts before the width is known.
    /// </summary>
    public bool WillBreak { get; protected init; }
}
