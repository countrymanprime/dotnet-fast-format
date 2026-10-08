class C
{
    void M()
    {
        Task.Run(() => { Work(); });
        Task.Run(async () =>
        {
            await Work();
        });
        items.ForEach(item => { Process(item); });
        services.AddSingleton<IFoo>(provider => { return new Foo(provider); });
        Execute(firstArgument, secondArgument, () => { Work(); });
        ExecuteWithAVeryLongMethodNameThatLeavesNoRoom(firstArgumentWithLongName, secondArgumentWithLongName, () => { Work(); });
        Outer(Inner(() => { Work(); }));
        Run(() => { A(); }, () => { B(); });
        var x = items.Where(i => i.IsValid).Select(i => { return i.Name; }).ToList();
        builder.Configure(options => { options.Name = "x"; }).Build().Run();
        app.MapGet("/", async (HttpContext context) => { await context.Response.WriteAsync("Hello"); });
        var result = await Task.Run(() => { return Compute(); });
        Register(name, delegate { Work(); });
        Callback(() => { });
        var measurements = _columns.Select(column => measurer.MeasureColumn(column, totalCellWidth, additionalArgument));
        Run(first, second, x => x.Something(argumentNumberOne, argumentNumberTwo, argumentNumberThree, four));
        var list = items.Where(item => item.IsActive && item.Count > threshold && item.Name != null && other).ToList();
        Foo(x => new Bar { A = x, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8, I = 9, J = 10, K = 11, L = 12 });
        Register(new Options { A = 1 }, new { Name = "a very long anonymous object member value goes here", Other = 2 });
        var types = new DynamicMethod(commandType.Name + "_init", null, new Type[] { typeof(IDbCommand) }, typeof(Foo));
        Wrap(first, second, x => x.IsSomethingWithAVeryLongName && x.IsSomethingElseWithAVeryLongName || x.Third);
    }
}
