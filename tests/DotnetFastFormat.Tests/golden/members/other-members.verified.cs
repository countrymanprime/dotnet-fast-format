enum Empty { }

enum Color : byte
{
    Red = 1,
    Green,
    Blue = Red | Green
}

[Flags]
public enum Options
{
    None = 0,

    // first
    A = 1, // trailing a
    B = 2 // last without a comma
}

enum Trailing
{
    A,
    B,
}

enum Commented
{
    A, /* c */ B
}

public delegate void Handler<T>(object sender, T args) where T : class;

delegate int Compute(int a, int b);

class C
{
    public event EventHandler Changed;
    event EventHandler Other, Another;

    public event EventHandler Custom
    {
        add
        {
            handler += value;
        }
        remove
        {
            handler -= value;
        }
    }

    public int this[int index]
    {
        get
        {
            return items[index];
        }
        set
        {
            items[index] = value;
        }
    }

    public string this[string key, int n] => Lookup(key, n);

    public static C operator +(C a, C b)
    {
        return new C();
    }

    public static bool operator ==(C a, C b) => Equals(a, b);

    public static bool operator <(C a, C b) => false;

    public static implicit operator int(C value) => 0;

    public static explicit operator C(int value)
    {
        return new C();
    }

    ~C()
    {
        Cleanup();
    }
}
