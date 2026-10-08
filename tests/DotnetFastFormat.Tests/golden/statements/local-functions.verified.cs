class C
{
    void M()
    {
        int Local(int x)
        {
            return x * 2;
        }
        int Expr(int x) => x * 2;
        static async Task<int> Gen<T>(T value) where T : class
        {
            await Task.Yield();
            return 1;
        }
        void Wrapped(
            CancellationToken cancellationToken,
            string firstParameterWithALongName,
            int second
        )
        { }
        [Foo]
        void WithAttr() { }
        Local(1);
    }
}
