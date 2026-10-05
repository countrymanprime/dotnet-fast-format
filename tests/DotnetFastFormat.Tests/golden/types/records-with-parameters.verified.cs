public record Options(
    string Name = "x",
    int Count = -1,
    bool Flag = default,
    Kind Kind = Kind.Default
);

public record Wrapped(
    int[] Values,
    Dictionary<string, List<int>> Map,
    (int A, int B) Pair,
    int? Maybe
);

public record VeryLongRecordNameForWrapping(
    string FirstParameterName,
    string SecondParameterName,
    int ThirdParameter
);

public record Expression(int X = 1 + 2);
