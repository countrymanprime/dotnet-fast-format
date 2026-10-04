namespace DotnetFastFormat.Tests;

public class InvariantsTests
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
    public void AcceptsIdentityFormatter() =>
        Assert.Equal(Source, Invariants.FormatAndCheck(TestFormatters.Identity, Source));

    [Fact]
    public void RejectsNonIdempotentFormatter() =>
        AssertViolation(Invariant.Idempotent, TestFormatters.NonIdempotent);

    [Fact]
    public void RejectsTreeChange() =>
        AssertViolation(Invariant.TreePreserving, TestFormatters.RenamesIdentifier);

    [Fact]
    public void RejectsDroppedComments() =>
        AssertViolation(Invariant.NoLoss, TestFormatters.DropsComments);

    [Fact]
    public void RejectsCommentMovedAcrossCode() =>
        AssertViolation(Invariant.NoLoss, TestFormatters.MovesComment);

    [Fact]
    public void RejectsOutputWithNewDiagnostics() =>
        AssertViolation(Invariant.ValidOutput, TestFormatters.InvalidOutput);

    [Fact]
    public void IgnoresWhitespaceOnlyChanges()
    {
        var reindent = new TestFormatters.Delegate(s => s.Replace("    /* block */", "  /* block */", StringComparison.Ordinal));
        Invariants.FormatAndCheck(reindent, Source);
    }

    private static void AssertViolation(Invariant expected, DotnetFastFormat.Core.IFormatter formatter)
    {
        var ex = Assert.Throws<InvariantViolationException>(() => Invariants.FormatAndCheck(formatter, Source));
        Assert.Equal(expected, ex.Invariant);
    }
}
