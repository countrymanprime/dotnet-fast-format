namespace DotnetFastFormat.Core.Printers;

/// <summary>Where an expression sits, because a few layouts depend on it (ADR 0013).</summary>
internal enum ExprContext
{
    /// <summary>Anywhere not listed below: operands, arguments, return values.</summary>
    Default,

    /// <summary>The whole of an expression statement.</summary>
    Statement,

    /// <summary>Inside the parentheses of a header such as <c>if (...)</c>, whose group already indents its content.</summary>
    Condition,

    /// <summary>The value of an assignment, declarator or lambda body, where the layout already indents what breaks.</summary>
    Assigned,
}
