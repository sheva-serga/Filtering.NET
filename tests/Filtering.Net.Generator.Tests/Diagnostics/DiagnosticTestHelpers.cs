using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>
/// Asserts presence/absence of analyzer diagnostics emitted by FilterGenerator
/// against a small consumer source.
/// </summary>
internal static class DiagnosticTestHelpers
{
    public static void AssertDiagnostic(string sourceCode, string diagnosticId, bool excludeEntityFrameworkCore = false)
    {
        // Arrange
        var runResult = GeneratorRunner.RunDriver(
            sourceCode,
            excludeDiAbstractions: false,
            excludeEntityFrameworkCore: excludeEntityFrameworkCore).GetRunResult();

        // Act
        var observedIds = runResult.Diagnostics.Select(diagnostic => diagnostic.Id);

        // Assert
        observedIds.Should().Contain(diagnosticId, because: $"source should produce diagnostic {diagnosticId}");
    }

    public static void AssertNoDiagnostic(string sourceCode, string diagnosticId, bool excludeEntityFrameworkCore = false)
    {
        // Arrange
        var runResult = GeneratorRunner.RunDriver(
            sourceCode,
            excludeDiAbstractions: false,
            excludeEntityFrameworkCore: excludeEntityFrameworkCore).GetRunResult();

        // Act
        var observedIds = runResult.Diagnostics.Select(diagnostic => diagnostic.Id);

        // Assert
        observedIds.Should().NotContain(diagnosticId, because: $"clean source should not produce diagnostic {diagnosticId}");
    }

    public static void AssertNoDiagnostics(string sourceCode, bool excludeEntityFrameworkCore = false)
    {
        // Arrange
        var runResult = GeneratorRunner.RunDriver(
            sourceCode,
            excludeDiAbstractions: false,
            excludeEntityFrameworkCore: excludeEntityFrameworkCore).GetRunResult();

        // Act
        var observed = runResult.Diagnostics;

        // Assert
        observed.Should().BeEmpty();
    }

    /// <summary>
    /// Returns the full set of diagnostics produced by running the generator against
    /// <paramref name="sourceCode"/>. Prefer <see cref="AssertDiagnostic"/> /
    /// <see cref="AssertNoDiagnostic"/> for simple presence checks; use this overload
    /// when the test also needs to inspect diagnostic message content.
    /// </summary>
    public static IReadOnlyList<Diagnostic> GetDiagnostics(string sourceCode, bool excludeEntityFrameworkCore = false)
    {
        var runResult = GeneratorRunner.RunDriver(
            sourceCode,
            excludeDiAbstractions: false,
            excludeEntityFrameworkCore: excludeEntityFrameworkCore).GetRunResult();

        return runResult.Diagnostics;
    }
}
