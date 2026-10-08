namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// A choice between layouts of the same content, tried in order: the first that fits on the line is printed flat
/// (a layout that contains a forced break fits when the text before that break fits), and when none fits the last one
/// is printed broken. It is how a call whose last argument is a lambda with a body keeps the call's opening
/// line (ADR 0013). It does not make its enclosing groups break, even when its first layout contains a forced break.
/// </summary>
internal sealed class ConditionalGroupDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="ConditionalGroupDoc"/> class.</summary>
    /// <param name="states">The layouts, from the one preferred when it fits to the one that always works. At least one.</param>
    public ConditionalGroupDoc(IReadOnlyList<Doc> states)
    {
        if (states.Count == 0)
        {
            throw new ArgumentException("A conditional group needs at least one layout.", nameof(states));
        }

        States = states;
        WillBreak = states[0].WillBreak;
    }

    /// <summary>Gets the layouts, most preferred first.</summary>
    public IReadOnlyList<Doc> States { get; }
}
