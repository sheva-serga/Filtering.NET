namespace Filtering.Net.Generator;

// Step names consumed by tests via GeneratorDriverRunResult.Results[].TrackedSteps[name].
internal static class TrackingNames
{
    internal const string GeneratorIndex = nameof(GeneratorIndex);
    internal const string FilterClassDeclarations = nameof(FilterClassDeclarations);
    internal const string FilterClassModels = nameof(FilterClassModels);
    internal const string ResolvedFilterClassModels = nameof(ResolvedFilterClassModels);
}
