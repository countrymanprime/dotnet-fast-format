using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints a whole file: its directives, attributes and declarations.</summary>
internal static class CompilationUnitPrinter
{
    /// <summary>Prints <paramref name="root"/> with a final line break.</summary>
    /// <param name="root">The parsed file, which has at least one declaration and no conditional directives.</param>
    /// <returns>The document for the file.</returns>
    public static Doc Print(CompilationUnitSyntax root)
    {
        var children = new List<SyntaxNode>();
        children.AddRange(root.Externs);
        children.AddRange(root.Usings);
        children.AddRange(root.AttributeLists);
        children.AddRange(root.Members);
        children.Sort((a, b) => a.SpanStart.CompareTo(b.SpanStart));
        return Docs.Concat(MemberList.Print(children), Docs.HardLine);
    }
}
