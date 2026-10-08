class C
{
    void M()
    {
        var names = people.Where(p => p.Age > 18).Select(p => p.Name).OrderBy(n => n).ToList();
        var result = collection
            .Where(item => item.IsActive)
            .Select(item => item.DisplayName)
            .OrderBy(name => name)
            .ToList();
        builder.Append("a").Append("b").Append("c");
        builder
            .Append("first part of the text")
            .Append("second part of the text")
            .Append("third part of the text")
            .AppendLine();
        Console.WriteLine(string.Join(", ", values));
        this.items.Add(item);
        return string.Join(
            ",",
            Enumerable.Range(1, count).Select(index => index.ToString()).ToArray(),
            another
        );
        services
            .AddSingleton<IFoo, Foo>()
            .AddScoped<IBar, Bar>()
            .AddTransient<IBaz, Baz>()
            .AddLogging();
        var x = Foo.Bar.Baz.Qux(
            argumentNumberOne,
            argumentNumberTwo,
            argumentNumberThree,
            argumentNumberFour,
            five
        );
        var z = a.b.c.d.e.f;
        var q = first?.Second?.Third
            ?.Method()
            ?.Another()
            ?.Final?.ToString()
            ?.Trim()
            ?.ToUpperInvariant()?.Length;
        var shortChain = a.B().C();
        var viaIndex = lookup[key].Items
            .First()
            .Name.Trim()
            .Split(',')
            .Select(part => part.Trim())
            .Last().Value;
        var factory = Create(argumentOne)
            .Configure(optionOne)
            .Configure(optionTwo)
            .Configure(optionThree)
            .Build();
        var tail = someObject.Method(
            argumentNumberOne,
            argumentNumberTwo
        ).Property.AnotherProperty.Final;
    }
}
