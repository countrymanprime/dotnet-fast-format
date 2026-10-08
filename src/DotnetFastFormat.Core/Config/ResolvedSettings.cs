namespace DotnetFastFormat.Core.Config;

/// <summary>What <c>.editorconfig</c> files say about one source file.</summary>
/// <param name="Options">The formatter settings: every supported key that applies, and the defaults for the rest.</param>
/// <param name="Warnings">
/// Messages about the configuration this file's settings come from: an <c>.editorconfig</c> that could not be read or
/// has lines that are not valid, and values that were ignored (CFG-010, CFG-011). The same message is returned for every
/// file the configuration applies to, so a caller that prints them removes duplicates.
/// </param>
public sealed record ResolvedSettings(FormatOptions Options, IReadOnlyList<string> Warnings);
