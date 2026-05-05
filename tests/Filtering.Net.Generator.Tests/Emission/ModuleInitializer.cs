using System.Runtime.CompilerServices;

namespace Filtering.Net.Generator.Tests.Emission;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
