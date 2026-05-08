using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Filtering.Net.Generator.Tests.Resolution;

internal static class ResolutionTestHelpers
{
    public static NestedFilterResolver.ResolvedHost Resolve(string sourceCode, string hostFilterClassName)
    {
        var compilation = GeneratorRunner.BuildCompilation(sourceCode);
        var allExtractionResults = GeneratorRunner.ExtractFilterClassResults(sourceCode).ToImmutableArray();
        var hostExtractionResult = allExtractionResults
            .First(extractionResult =>
                extractionResult.Model is not null && extractionResult.Model.ClassName == hostFilterClassName);

        return NestedFilterResolver.Resolve(
            hostExtractionResult,
            allExtractionResults,
            compilation,
            CancellationToken.None);
    }
}
