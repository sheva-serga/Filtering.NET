using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>
/// Asserts that running FilterGenerator over the given consumer source produces output
/// that compiles cleanly under the test compilation (no error-severity diagnostics).
/// </summary>
internal static class CompileVerifier
{
    public static void AssertCompilesCleanly(
        string consumerSource,
        bool excludeEntityFrameworkCore = false)
    {
        // Arrange + Act
        // The deleted CompileEmittedCodeHarness used the FULL TPA reference graph (including
        // Microsoft.Extensions.DependencyInjection.Abstractions) so DI-using emitted code
        // would compile. Pass excludeDiAbstractions: false to preserve that behavior.
        var (_, updatedCompilation) = GeneratorRunner.RunAndUpdate(
            consumerSource,
            excludeDiAbstractions: false,
            excludeEntityFrameworkCore: excludeEntityFrameworkCore);

        // Assert
        var compilationDiagnostics = updatedCompilation.GetDiagnostics();
        compilationDiagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty(because: "the emitted source plus the test compilation should compile without errors");
    }
}
