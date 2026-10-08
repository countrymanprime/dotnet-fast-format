[Serializable]
[Obsolete("x")]
[Foo]
public class C
{
    [Fact]
    public void Test() { }

    [Fact]
    [Trait("a", "b")]
    public async Task Other()
    {
        Run();
    }

    [JsonProperty("x")]
    public int X { get; set; }

    [Obsolete]
    private int field = 1;

    [Foo]
    public C()
        : base() { }

    // comment between
    [Bar]
    public int Y => 2;

    [Foo(1, 2)]
    void Multi() { }

    [A]
    // comment after attribute
    void Commented() { }

    int Z
    {
        [Foo]
        get;
        set;
    }

    [A]
    int Same;
    [A] // trailing after attribute
    int Trailing;
}
