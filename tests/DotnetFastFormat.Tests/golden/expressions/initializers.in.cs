class C
{
    void M()
    {
        var a = new Foo { FirstProperty = firstValueWithALongName, SecondProperty = secondValueWithALongName, Third = 3 };
        var b = new Foo
        {
            A = 1,
            B = 2,
        };
        var c = new[] { "first string that is long", "second string that is long", "third string that is long" };
        var d = new List<int> { 1, 2, 3, };
        var e = new { FirstProperty = firstValueWithALongName, SecondProperty = secondValueWithALongName, Third = 3 };
        var f = [firstElementWithALongName, secondElementWithALongName, thirdElementWithALongName, fourthElement];
        var g = new Outer { Inner = new Inner { Value = 1 }, Items = { 1, 2 }, [0] = 5 };
        var h = new Dictionary<string, List<int>>
        {
            ["first"] = new List<int> { 1, 2, 3 },
            ["second"] = new List<int> { 4, 5, 6 },
        };
        Register(new Options { Name = "name", Description = "a description that is long enough to wrap the line" });
        Register(firstArgument, new Options { Name = "name", Description = "a description that is long enough" });
        var i = new Foo
        {
            Nested = new Bar { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8, I = 9, J = 10, K = 11, L = 12 },
        };
        var j = x with { FirstProperty = firstValueWithALongName, SecondProperty = secondValueWithALongName, T = 3 };
    }
}
