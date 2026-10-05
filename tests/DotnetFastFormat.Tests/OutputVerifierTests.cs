using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

public class OutputVerifierTests
{
    private const string Source = """
        // header comment
        namespace N;

        class Foo
        {
            /* block */ void M() { } // trailing
        }
        #if DEBUG
        #endif

        """;

    [Fact]
    public void IdenticalTextPasses() =>
        Assert.True(OutputVerifier.Verify(Source, Source).Succeeded);

    [Fact]
    public void WhitespaceOnlyChangesPass() =>
        Assert.True(OutputVerifier.Verify(Source, Source.Replace("    /* block */", "  /* block */", StringComparison.Ordinal)).Succeeded);

    [Fact]
    public void ReindentingAContinuationLineOfAMultiLineCommentPasses()
    {
        const string Before = "class C\n{\n    /* a\n       b */\n    int x;\n}\n";
        const string After = "class C\n{\n    /* a\n  b */\n    int x;\n}\n";

        Assert.True(OutputVerifier.Verify(Before, After).Succeeded);
    }

    [Fact]
    public void ANewSyntaxErrorIsReported()
    {
        VerificationResult result = OutputVerifier.Verify(Source, Source + "}");

        Assert.Equal(OutputInvariant.ValidOutput, result.Violation);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void ErrorsThatAlreadyExistInTheInputAreNotNew()
    {
        const string Broken = "class C { void M( }";

        Assert.True(OutputVerifier.Verify(Broken, Broken).Succeeded);
    }

    [Fact]
    public void ARenamedIdentifierIsReported() =>
        Assert.Equal(OutputInvariant.TreePreserving, OutputVerifier.Verify(Source, Source.Replace("Foo", "Bar", StringComparison.Ordinal)).Violation);

    [Fact]
    public void ADroppedTokenIsReported() =>
        Assert.Equal(OutputInvariant.TreePreserving, OutputVerifier.Verify("class C { int x; int y; }", "class C { int x; }").Violation);

    [Fact]
    public void ADroppedCommentIsReported() =>
        Assert.Equal(OutputInvariant.NoLoss, OutputVerifier.Verify(Source, Source.Replace("// header comment\n", string.Empty, StringComparison.Ordinal)).Violation);

    [Fact]
    public void AChangedCommentIsReported() =>
        Assert.Equal(OutputInvariant.NoLoss, OutputVerifier.Verify(Source, Source.Replace("header comment", "other comment", StringComparison.Ordinal)).Violation);

    [Fact]
    public void ACommentMovedAcrossCodeIsReported()
    {
        const string Moved = "namespace N;\n\nclass Foo\n{\n    /* block */ void M() { } // trailing\n}\n// header comment\n#if DEBUG\n#endif\n";

        Assert.Equal(OutputInvariant.NoLoss, OutputVerifier.Verify(Source, Moved).Violation);
    }

    [Fact]
    public void ADroppedDirectiveIsReported() =>
        Assert.Equal(OutputInvariant.NoLoss, OutputVerifier.Verify(Source, Source.Replace("#if DEBUG\n#endif\n", string.Empty, StringComparison.Ordinal)).Violation);

    [Fact]
    public void ChangedStringContentsAreReported() =>
        Assert.Equal(OutputInvariant.TreePreserving, OutputVerifier.Verify("class C { string s = \"a  b\"; }", "class C { string s = \"a b\"; }").Violation);

    [Fact]
    public void BothSidesAreParsedWithTheSameLanguageVersion() =>
        Assert.Equal(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview, SourceParser.Options.LanguageVersion);
}
