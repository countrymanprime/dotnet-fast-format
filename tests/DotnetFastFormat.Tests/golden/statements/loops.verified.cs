class C
{
    async Task M()
    {
        for (int i = 0; i < 10; i++)
            Run(i);
        for (int i = 0, j = 10; i < j; i++, j--)
        {
            Run();
        }
        for (;;) { }
        for (; i < n;)
            i++;
        for (i = 0; ; i++) { }
        foreach (var x in xs)
            Run(x);
        foreach (var (key, value) in map)
        {
            Run(key, value);
        }
        await foreach (var x in stream) { }
        while (x)
            y();
        while (true)
        {
            Run();
        }
        do
            x();
        while (y);
        do
        {
            Run();
        }
        while (more);
        for (
            int index = 0;
            index < collectionWithALongName.Count && !cancellationToken.IsCancellationRequested;
            index++
        ) { }
        foreach (
            var itemWithALongName in someCollectionWithALongName
                .Where(x => x.IsValid)
                .Select(x => x.Value)
                .ToList()
        ) { }
        while (
            firstConditionWithALongName
            && secondConditionWithALongName
            && thirdConditionWithALongName
            && fourth
        ) { }
        for (var i = 0; i < n; i++) // comment
            Run();
        while (x)
            // above
            y();
        do
        {
            Run();
        }
        while (x); // after
    }
}
