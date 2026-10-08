[assembly: InternalsVisibleTo("Tests")]

[module: SkipLocalsInit]

class C
{
    [Theory]
    [InlineData(1, 2, Name = "x")]
    [InlineData(
        "a very long string that goes on and on",
        "another very long string that goes on",
        12345
    )]
    public void T(int a, int b) { }

    [A, B(1)]
    [return: MaybeNull]
    public int M(
        [In] int x,
        [Optional, DefaultParameterValue(0)] int y = 5,
        string s = "x",
        object o = default,
        int? n = null,
        CancellationToken ct = default(CancellationToken)
    )
    {
        return x;
    }

    public void Z(int[] a = null, params int[] rest) { }

    public static string Ext(this string value, int length = -1) => value;

    public void Q(string a = "x", Foo f = new(), int b = Max - 1, Action g = null /* why */) { }

#if DEBUG
    [Debuggable]
#endif
    public int WithDirective { get; set; }

#if A
    [A]
#endif
    [B]
    public class Nested { }

    public void Brace()
#if X
    // x
#endif
    {
        Run();
    }

    class WithComment
    { // after brace
        int x;
    }

    void AfterBrace()
    { // after brace of a method
        Run();
    }

    void Empty()
    { // only a comment after the brace
    }
}
