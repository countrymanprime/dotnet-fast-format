class C
{
    void M()
    {
        var a = new Foo();
        var b = new Foo(1, 2);
        var c = new List<Dictionary<string, int>>();
        Foo f = new();
        Foo g = new(1, 2);
        var d = new int[3];
        var e = new int[n, m];
        var h = new int[][] { new[] { 1 }, new int[0] };
        var i = new[] { 1, 2, 3 };
        var j = new int[] { };
        var k = new { A = 1, B = x, y };
        var l = new { };
        var m = [1, 2, ..rest];
        var n = [];
        var o = (1, "two");
        var p = (first: 1, second: "two");
        var q = x with { A = 1 };
        var r = new VeryLongTypeNameForTheCreation(
            firstArgumentWithLongName,
            secondArgumentWithLongName,
            third
        );
        var s = new Foo { A = 1, B = 2 };
        var t = new Foo() { A = 1 };
        var u = new Foo(1) { };
    }
}
