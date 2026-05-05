using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>
/// Compiles the generator output for a consumer source into an in-memory assembly and
/// returns the loaded <see cref="Assembly"/>. Throws when emission produces error diagnostics.
/// </summary>
internal static class RuntimeLoader
{
    public static Assembly LoadGeneratedAssembly(string consumerSource)
    {
        // The existing CompileAndLoad used the FULL TPA reference graph (including
        // Microsoft.Extensions.DependencyInjection.Abstractions) so DI-using emitted code
        // would compile. Pass excludeDiAbstractions: false to preserve that behavior.
        var (_, updatedCompilation) = GeneratorRunner.RunAndUpdate(
            consumerSource,
            excludeDiAbstractions: false);

        var emissionErrors = updatedCompilation
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (emissionErrors.Count > 0)
        {
            throw new InvalidOperationException(
                "Compilation of generated source failed: "
                + string.Join("; ", emissionErrors.Select(diagnostic => diagnostic.GetMessage())));
        }

        using var assemblyStream = new MemoryStream();
        var emitResult = updatedCompilation.Emit(assemblyStream);
        if (!emitResult.Success)
        {
            throw new InvalidOperationException(
                "Assembly.Emit failed: "
                + string.Join("; ", emitResult.Diagnostics.Select(diagnostic => diagnostic.GetMessage())));
        }
        return Assembly.Load(assemblyStream.ToArray());
    }
}
