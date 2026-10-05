class Config
{
    private   int   count ;
    private readonly List<string>   names =   new( ) ;
    public const   int   Max = 10 ,  Min = 1 ;
    int[] values;
    Dictionary<string,List<int>> map;
    (int A,int B) pair;



    public int Id { get ; set ; }
    public string Name { get; private set; } = "x";
    public int Total => this.count   + 1 ;
    public int Computed { get { return 1; } }

    [Obsolete]
    public int Old;
}
