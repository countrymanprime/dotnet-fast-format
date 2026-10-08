class C
{
    void M()
    {
        var a = b+c*d-e;
        var b = x&&y||z;
        var c = !flag;
        var d = - -x;
        var e = -(-x);
        var f = a  is   Foo;
        var g = a as Foo;
        var h = (int)value;
        var i = (a+b)*c;
        var j = cond?yes:no;
        var k = await task;
        var l = typeof( int );
        var m = default( T );
        var n = x ?? y ?? z;
        var o = a[^1];
        var p = list[1..^1];
        var q = value is not null;
        var r = value is Foo foo && foo.Bar>3;
        i++;
        --j;
        x+=1;
        y = z = 3;
        var longCondition = firstConditionWithALongName && secondConditionWithALongName || thirdConditionWithALongName && fourth;
        var longSum = firstOperandWithALongName + secondOperandWithALongName + thirdOperandWithALongName + fourthOperand1;
        var cond = isEnabledWithALongName ? valueWhenEnabledWithALongName : valueWhenNotEnabledWithALongNameXXXXXXXXXXXX;
        var nested = first ? second ? thirdValueWithALongName : fourthValueWithALongName : fifthValueWithALongNameXXXXXXXXXXXXXXXX;
        return firstConditionWithALongName && secondConditionWithALongName && thirdConditionWithALongName && fourthCondition;
        throw new InvalidOperationException("some message that is long enough to push the line past the width ok");
        SomeMethod(firstOperandWithALongName + secondOperandWithALongName + thirdOperandWithALongName + fourthOperandWithALong);
        value = condition ? FirstChoiceWithALongName(argumentOne, argumentTwo) : SecondChoiceWithALongName(argumentOne, argument);
        var coalesce = firstCandidateWithALongName ?? secondCandidateWithALongName ?? thirdCandidateWithALongName ?? fourth1;
        var s = x switch { 1 => "one", _ => "other" };
        var t = x switch
        {
            > 0 and < 10 => "small",
            _ when other => LongCall(argumentNumberOne, argumentNumberTwo, argumentNumberThree, argumentNumberFour, five),
            _ => throw new ArgumentOutOfRangeException(nameof(x)),
        };
    }
}
