using System.Runtime.CompilerServices;

namespace DotnetFastFormat.Tests;

internal static class ModuleInit
{
    [ModuleInitializer]
    public static void Init() => DiffEngine.DiffRunner.Disabled = true;
}
