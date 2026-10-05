namespace DotnetFastFormat.Core.Config;

/// <summary>A <c>[glob]</c> header and the pairs under it.</summary>
internal sealed class EditorConfigSection
{
    private readonly List<EditorConfigPair> pairs = [];

    /// <summary>Initializes a new instance of the <see cref="EditorConfigSection"/> class.</summary>
    /// <param name="name">The text between the brackets.</param>
    /// <param name="line">The line number of the header, counted from 1.</param>
    public EditorConfigSection(string name, int line)
    {
        Name = name;
        Line = line;
    }

    /// <summary>Gets the section name: a glob, as written.</summary>
    public string Name { get; }

    /// <summary>Gets the line number of the header, counted from 1.</summary>
    public int Line { get; }

    /// <summary>Gets the pairs of the section in file order. A repeated key appears repeatedly; the last one wins.</summary>
    public IReadOnlyList<EditorConfigPair> Pairs => pairs;

    /// <summary>Adds a pair at the end of the section.</summary>
    /// <param name="pair">The pair.</param>
    public void Add(EditorConfigPair pair) => pairs.Add(pair);
}
