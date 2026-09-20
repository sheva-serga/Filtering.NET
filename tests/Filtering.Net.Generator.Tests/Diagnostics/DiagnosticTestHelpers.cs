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

    public static void AssertDiagnosticWithLocations(
        string sourceCode,
        string diagnosticId,
        int primaryLine,
        int primaryColumn,
        params (int Line, int Column)[] additionalLines)
    {
        var diagnostics = GetDiagnostics(sourceCode);
        var matching = diagnostics.Where(diagnostic => diagnostic.Id == diagnosticId).ToList();

        matching.Should().NotBeEmpty(because: $"source should produce diagnostic {diagnosticId}");
        var diagnostic = matching[0];
        var primary = diagnostic.Location.GetLineSpan().StartLinePosition;
        primary.Line.Should().Be(primaryLine - 1, because: $"primary location line for {diagnosticId}");
        primary.Character.Should().Be(primaryColumn - 1, because: $"primary location column for {diagnosticId}");

        diagnostic.AdditionalLocations.Should().HaveCount(additionalLines.Length,
            because: $"{diagnosticId} should report {additionalLines.Length} additional locations");

        for (var index = 0; index < additionalLines.Length; index++)
        {
            var expected = additionalLines[index];
            var observed = diagnostic.AdditionalLocations[index].GetLineSpan().StartLinePosition;
            observed.Line.Should().Be(expected.Line - 1);
            observed.Character.Should().Be(expected.Column - 1);
        }
    }

    public static void AssertDiagnosticHasAdditionalLocations(string sourceCode, string diagnosticId, int expectedAdditionalCount)
    {
        var diagnostic = GetDiagnostics(sourceCode).First(d => d.Id == diagnosticId);
        diagnostic.AdditionalLocations.Should().HaveCount(expectedAdditionalCount);
    }
}
