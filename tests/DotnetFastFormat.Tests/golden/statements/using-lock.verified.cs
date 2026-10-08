class C
{
    async Task M()
    {
        using (var a = Open())
            Run(a);
        using (var a = Open())
        {
            Run(a);
        }
        using (var a = Open())
        using (var b = Open(a))
        {
            Run(a, b);
        }
        using (existing) { }
        await using (var c = OpenAsync()) { }
        lock (gate)
            count++;
        lock (gate)
        {
            count++;
        }
        unsafe
        {
            Run();
        }
        checked
        {
            Run();
        }
        fixed (int* p = array)
        {
            Run(p);
        }
        using (
            var connectionWithALongName = connectionFactory.CreateConnection(
                connectionString,
                timeout
            )
        ) { }
        goto done;
        done:
        Run();
        if (a)
            goto case 1;
    }
}
