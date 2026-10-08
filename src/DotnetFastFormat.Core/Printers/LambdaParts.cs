using DotnetFastFormat.Core.Layout;

namespace DotnetFastFormat.Core.Printers;

/// <summary>The two halves of a lambda with an expression body, so a call can lay them out in more than one way.</summary>
/// <param name="Header">The modifiers, parameters and <c>=&gt;</c>.</param>
/// <param name="Body">The expression after the arrow.</param>
internal sealed record LambdaParts(Doc Header, Doc Body);
