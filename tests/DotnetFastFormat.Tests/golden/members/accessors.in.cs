class C
{
    int x;
    public int X { get { return x; } set { x = value; } }
    public int Y { get => y; set => y = value; }
    public int Z { get; private set; }
    public int W { get; init; } = 5;
    public int V
    {
        get { return v; }
        // comment
        set { v = value; }
    }
    public int U { get { return 1; } }

    public int T
    {
        get;
        set { }
    }
    public int S { get => LongExpression(argumentNumberOne, argumentNumberTwo, argumentNumberThree, argumentFour); }
    public List<string> Names { get; } = new List<string> { "first", "second" };
    public string Description { get; set; } = "a long default value for the description of this property that is long";
    public int Last
    {
        get { return 1; } // trailing
        // before close
    }
}
