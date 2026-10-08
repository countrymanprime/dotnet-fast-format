class C
{
    string M(string value, string first, string second)
    {
        var regular = "regular   string";
        var verbatim = @"verbatim
   string with   spaces
and a ""quote""";
        var interpolated = $"interpolated {value,10:N2} and   {other}";
        var interpolatedVerbatim = $@"line one {first}
   line two {second}";
        var raw = """
            raw   string
              keeps  shape
            """;
        var rawInterpolated = $$"""
            {{value}} and   {single}
            """;
        Log(@"a
b", second);
        Log(first, @"a
b");
        Log("a" + @"b
c" + "d");
        var x = Format(@"SELECT *
   FROM t", argumentOne, argumentTwo);
        var veryLongStringLiteral =
            "this is a very long string literal that will not be wrapped or reflowed because string contents are never touched";
        Call(
            "this is a very long string literal that will not be wrapped or reflowed because it is long",
            other
        );
        var chain = @"multi
line".Trim().ToUpper();
        var list = new[] { @"a
b", "c" };
        var sql =
            $@"SELECT {columns}
FROM {table}"
            + suffixWithALongName
            + anotherSuffixWithALongName
            + yetAnotherSuffixWithALongName
            + last;
        return @"x
y";
    }
}
