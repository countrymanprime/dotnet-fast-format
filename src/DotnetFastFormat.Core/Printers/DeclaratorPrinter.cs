using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints variable declarations: the type and the declarators of a local, field or <c>for</c> initializer.</summary>
internal static class DeclaratorPrinter
{
    /// <summary>Prints <c>modifiers Type a = 1, b = 2</c> without the semicolon.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="prefix">The tokens before the type (modifiers, <c>using</c>, <c>await</c>).</param>
    /// <param name="builder">Collects the declaration's own tokens.</param>
    /// <returns>The document; the builder is marked unsafe when a declarator has syntax this printer leaves as written.</returns>
    public static Doc Declaration(VariableDeclarationSyntax declaration, IEnumerable<SyntaxToken> prefix, NodeBuilder builder)
    {
        Doc head = builder.Tokens([.. prefix, .. declaration.Type.DescendantTokens()]);
        var parts = new List<Doc> { head, Docs.Text(" ") };
        SeparatedSyntaxList<VariableDeclaratorSyntax> variables = declaration.Variables;
        for (int i = 0; i < variables.Count; i++)
        {
            if (i > 0)
            {
                parts.Add(builder.Token(variables.GetSeparator(i - 1)));
                parts.Add(Docs.Text(" "));
            }

            parts.Add(Declarator(variables[i], builder));
        }

        return Docs.Concat(parts);
    }

    /// <summary>Prints <c>name = value</c>.</summary>
    /// <param name="variable">The declarator.</param>
    /// <param name="builder">Collects the declarator's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Declarator(VariableDeclaratorSyntax variable, NodeBuilder builder)
    {
        if (variable.ArgumentList is not null)
        {
            builder.Reject();
        }

        Doc name = builder.Token(variable.Identifier);
        return variable.Initializer is null
            ? name
            : AssignmentLayout.Print(name, variable.Initializer.EqualsToken, variable.Initializer.Value, builder);
    }
}
