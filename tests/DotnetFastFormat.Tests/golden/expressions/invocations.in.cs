class C
{
    void M()
    {
        Foo( a ,b,   c );
        Foo();
        Foo(  );
        this.Bar(x:1,y: 2);
        int.TryParse(text, out var value);
        Process(ref  a,in b, out c);
        var v = items[ 0 ];
        var w = matrix[i,  j];
        var n = obj?.Name;
        var m = obj?.Items?[0];
        var k = maybe!.Value;
        Generic<int,   string>(first, second);
        WriteSomethingVeryLongToTheConsole(firstArgumentWithLongName, secondArgumentWithLongName, thirdArgument);
        Outer(Inner(firstArgumentWithLongName, secondArgumentWithLongName), anotherArgumentWithLongName1);
        VeryLongMethodNameNumberOne(VeryLongMethodNameNumberTwo(argumentNumberOne, argumentNumberTwo, argumentNumberThree));
        var longValue = SomeStaticClass.SomeMethodWithALongName(firstArgumentWithLongName, secondArgumentWithLongName, third);
        var v = SomeStaticClass.SomeMethod(firstArgumentWithLongNam, secondArgumentWithLongName, t);
        var v = SomeStaticClass.SomeMethod(firstArgumentWithLongName, secondArgumentWithLongName, t);
        Call(argumentNumberOne: valueNumberOne, argumentNumberTwo: valueNumberTwo, argumentNumberThree: valueNumberThree);
        var element = SomeDictionaryWithALongName[SomeKeyExpressionWithALongName(argumentNumberOne, argumentNumberTwo)];
    }
}
