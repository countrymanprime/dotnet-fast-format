class A
{
    void M(int a, /* inline */ int b) { }

    int   x /* before semicolon */ ;

    void N()
    // between header and body
    { }

    int y; /* a */ // b
    int z;
}

class B // after header
{
    int w;
}

class C { // after brace
    int v;
}
