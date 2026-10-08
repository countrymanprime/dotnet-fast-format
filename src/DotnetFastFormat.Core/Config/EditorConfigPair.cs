namespace DotnetFastFormat.Core.Config;

/// <summary>One <c>key = value</c> line of an <c>.editorconfig</c> section.</summary>
/// <param name="Key">The key, lowercased.</param>
/// <param name="Value">The value, trimmed, in the case it was written.</param>
/// <param name="Line">The line number, counted from 1.</param>
internal readonly record struct EditorConfigPair(string Key, string Value, int Line);
