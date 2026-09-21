using System.Collections.Immutable;

namespace Filtering.Net.Generator.Tests.Resolution;

internal static class ResolutionTestHelpers
{
    public static NestedFilterResolver.ResolvedHost Resolve(string sourceCode, string hostFilterClassName)
    {
        var allExtractionResults = GeneratorRunner.ExtractFilterClassResults(sourceCode).ToImmutableArray();
        var hostExtractionResult = allExtractionResults
            .First(extractionResult =>
                extractionResult.Model is not null && extractionResult.Model.ClassName == hostFilterClassName);

        return NestedFilterResolver.Resolve(
            hostExtractionResult,
            allExtractionResults,
            CancellationToken.None);
    }
}
