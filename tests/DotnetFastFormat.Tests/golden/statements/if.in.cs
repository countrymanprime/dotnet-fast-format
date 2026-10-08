class C
{
    void M()
    {
        if(a)b();
        if (a) { b(); }
        if (a) b(); else c();
        if (a)
        {
            b();
        }
        else if (c)
        {
            d();
        }
        else
        {
            e();
        }
        if (a) { }
        if (a) { } else { }
        if (a) return;
        else if (b) return 1;
        else throw new Exception();
        if (firstConditionWithALongName && secondConditionWithALongName && thirdConditionWithALongName && fourth)
        {
            Run();
        }
        if (SomeMethodWithALongName(argumentNumberOne, argumentNumberTwo, argumentNumberThree, argumentNumberFour))
            Run();
        if (a) // after condition
        {
        }
        if (a)
        {
        } // after block
        else
        {
        }
        if (a)
        {
            b();
        }
        // between
        else
        {
        }
        if (a)
            // above statement
            b();
        if (a) { b(); } // trailing
        if (a)
        {
            if (b) c();
        }
        else
            d();
        int after = 1;
    }
}
