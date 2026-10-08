class C
{
    IEnumerable<int> Gen()
    {
        yield return 1;
        yield break;
    }

    void Throws()
    {
        throw new Exception("x");
    }

    async Task Locals()
    {
        const int Max = 10;
        int a = 1, b = 2, c;
        using var file = Open();
        await using var x = Open();
        ;
        goto done;
        done:
        return;
    }

    void Unsupported()
    {
        int before = 1;
        checked
        {
            before++;
        }
        var query = from   x in xs   where x>1   select   x;
        int after = 2;
    }
}
