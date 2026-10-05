class Calc
{
    public int Add(int a, int b)
    { return a+b; }

    public int Twice(int x) => x*2;

    void Nothing() { }

    public static async Task<IReadOnlyList<KeyValuePair<string, int>>> ComputeEverythingAsync(
        CancellationToken cancellationToken,
        string prefix = "p",
        int retries = 3
    )
    { await Task.Yield(); }

    public T Pick<T>(T a, T b) where T : class, IComparable<T>
    { return a; }

    void Wrapped<TFirst, TSecond>(TFirst first, TSecond second)
        where TFirst : class
        where TSecond : struct, IEquatable<TSecond>
    { }

    string IFoo.Name() => "n";
}

interface IFoo
{
    string Name();
    void Run(int a);

    int Count { get; }
    int Size { get; set; }
}
