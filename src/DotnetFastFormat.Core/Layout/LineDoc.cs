namespace DotnetFastFormat.Core.Layout;

/// <summary>A place where a line may break.</summary>
internal sealed class LineDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="LineDoc"/> class.</summary>
    /// <param name="kind">What the line prints when flat.</param>
    public LineDoc(LineKind kind)
    {
        Kind = kind;
        ForcesBreak = kind == LineKind.Hard;
    }

    /// <summary>Gets what the line prints when flat.</summary>
    public LineKind Kind { get; }
}
