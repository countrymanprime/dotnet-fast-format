namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// One of two documents, chosen by the mode of the group it sits in: <see cref="BreakContents"/> when that
/// group is broken, <see cref="FlatContents"/> when it is flat. It never forces a group to break by itself.
/// </summary>
internal sealed class IfBreakDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="IfBreakDoc"/> class.</summary>
    /// <param name="breakContents">Printed when the enclosing group is broken.</param>
    /// <param name="flatContents">Printed when the enclosing group is flat.</param>
    public IfBreakDoc(Doc breakContents, Doc flatContents)
    {
        BreakContents = breakContents;
        FlatContents = flatContents;
    }

    /// <summary>Gets the document printed when the enclosing group is broken.</summary>
    public Doc BreakContents { get; }

    /// <summary>Gets the document printed when the enclosing group is flat.</summary>
    public Doc FlatContents { get; }
}
