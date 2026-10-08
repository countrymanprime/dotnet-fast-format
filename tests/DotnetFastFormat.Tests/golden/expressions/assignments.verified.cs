class C
{
    void M()
    {
        longTargetName =
            firstConditionWithALongName
            && secondConditionWithALongName
            && thirdConditionWithALongName;
        string s =
            "a long string literal that does not fit on the line with its target and keeps going to the end";
        var v = @"a verbatim
string";
        var w =
            Some.Long.Property.Chain.That.Does.Not.Fit.On.One.Line.With.The.Target.Name.And.More.Segments.Here;
        var x =
            SomeMethodCall(argument1, argument2)
            + AnotherMethodCall(argument3, argument4)
            + OneMore(argument5);
        x +=
            yetAnotherOperandWithALongName
            + andAnotherOperandWithALongName
            + aThirdOperandWithALongNameXYZ;
        var y =
            isSomethingTrueAboutThis && isSomethingElseTrue
                ? resultWhenTrueXXXXXXXXXXXXXX
                : resultWhenFalseXXXXXXXX;
        var z = condition
            ? shortWhenTrue
            : veryLongWhenFalseExpressionThatGoesOnAndOnAndOnAndOnAndOnAndOnAndOnAndOn;
        var obj = new SomeClass
        {
            Property = firstValueWithALongName,
            Other = secondValueWithALongName,
            Third = third
        };
        a = b = c;
        var (p, q) = GetPair();
        var f = (Func<int, int>)(x => x + 1);
        var nestedInitializer = new Foo
        {
            Nested = new Bar
            {
                A = 1,
                B = 2,
                C = 3,
                D = 4,
                E = 5,
                F = 6,
                G = 7,
                H = 8,
                I = 9,
                J = 10,
                K = 11,
                L = 12
            },
        };
    }
}
