class C
{
    void M()
    {
        // leading
        int a = 1; // trailing

        /* block */
        int b = 2;
        int c = /* inline */ 3;
        int d = 4;
#if DEBUG
        int f = 6;
#endif
        return;
        // before close
    }

    void Empty()
    {
        // only a comment
    }

    void Nested()
    {
        { // after brace
            int x = 1;
        }
        {
            int y = 2;
        } // after nested
        int z = 3; // z
    }
}
