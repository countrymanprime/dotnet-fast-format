namespace DotnetFastFormat.Core;

/// <summary>How one level of indentation is written.</summary>
public enum IndentStyle
{
    /// <summary>Spaces only.</summary>
    Space,

    /// <summary>Tab characters, plus spaces for a remainder smaller than the tab width.</summary>
    Tab,
}
