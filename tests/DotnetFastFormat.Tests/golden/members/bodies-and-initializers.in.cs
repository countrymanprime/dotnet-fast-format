class C
{
    private readonly int count = 1  +  2;
    private string name = "a long string literal that does not fit on the line with its target and keeps going on";
    private static readonly Dictionary<string, int> Map = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };
    int[] numbers = { 1, 2, 3 };
    const int Max = 10, Min = 1;
    public int Total => first   +second;
    public string Describe(int value) => "the value of the argument is " + value + " and that is the whole story told";
    public int Call() => SomethingLong(argumentNumberOne, argumentNumberTwo, argumentNumberThree, argumentFour);
    public Foo Create() => new Foo { A = 1, B = 2, Third = thirdValueWithALongName, Fourth = fourthValueWithALongName };
    public C(int a, int b) : base(a, b) { }
    public C(int a) : this(a, 0) { Run(); }
    public C(int firstParameterWithALongName, int secondParameterWithALongName) : base(firstParameterWithALongName, secondParameterWithALongName) { }
    public int Computed => condition ? valueWhenTrueWithALongName : valueWhenFalseWithALongNameThatKeepsGoing1234567;
}
