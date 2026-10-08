class C
{
    void M()
    {
        Action a = () => { };
        Action b = () => Run();
        Func<int,int> c = x=>x+1;
        Func<int,int,int> d = (x,y)=>x+y;
        Func<int,int> e = async x => await Task.FromResult(x);
        var f = list.Select(x => x * 2);
        Action g = () =>
        {
            Run();
        };
        var h = (int x) => x;
        var i = static (x, y) => x + y;
        var j = delegate { Run(); };
        var k = delegate (int x) { return x; };
        Func<string> l = () => veryLongFunctionNameNumberOne(argumentNumberOne, argumentNumberTwo) + anotherCall(argument);
        Func<int, int> m = parameterWithALongName => parameterWithALongName * factorWithALongName + offsetWithALongName1;
        Action<int> n = x =>
        {
            if (x > 0) { Run(); }
        };
    }
}
