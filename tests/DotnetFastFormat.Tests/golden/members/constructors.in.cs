class Widget
{
    public Widget( ) { }
    public Widget(int size) : this(size, "w") { }
    public Widget(int size, string name)
        : base( )
    {
        Size = size;
    }
    static Widget( ) { Init(); }
    protected Widget(VeryLongTypeNameNumberOne firstArgument, VeryLongTypeNameNumberTwo secondArgument) { }
}
