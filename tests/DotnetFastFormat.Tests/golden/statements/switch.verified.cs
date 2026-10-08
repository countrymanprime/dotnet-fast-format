class C
{
    int M(int x)
    {
        switch (x)
        {
            case 1:
                Run();
                break;
            default:
                break;
        }
        switch (x)
        {
            case 1:
            case 2:
                Run();
                break;

            case 3:
            {
                Run();
                break;
            }
            case 4:
            {
                Run();
                break;
            }
            case > 5 and < 10 when other:
                return 1;
            case Foo f:
                return 2;
            default:
                throw new Exception();
        }
        switch (x) { }
        switch (a, b)
        {
            case (1, 2):
                break;
        }
        switch (x)
        {
            // leading comment
            case 1:
                // inside
                Run(); // trailing
                break;
            // between sections
            case 2: // after label
                break;
            default:
                break;
            // before close
        }
        switch (x)
        {
            case 1:
                Run(); // trailing of last
        }
        return 0;
    }
}
