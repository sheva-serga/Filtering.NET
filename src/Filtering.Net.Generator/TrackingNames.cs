namespace Filtering.Net.Generator;

/// <summary>
/// String constants used as incremental-pipeline step tracking names. Consumed by tests via
/// <c>GeneratorDriverRunResult.Results[].TrackedSteps[name]</c> to extract intermediate model
/// values without re-running the full generator.
/// </summary>
internal static class TrackingNames
{
    /// <summary>The pipeline step that produces <see cref="FilterClassModelWithDiagnostics"/> entries
    /// from <c>[GenerateFilter&lt;TEntity&gt;]</c>-decorated partial classes.</summary>
    internal const string FilterClassModels = nameof(FilterClassModels);
}
