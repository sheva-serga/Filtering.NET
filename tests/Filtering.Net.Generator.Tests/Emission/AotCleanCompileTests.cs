using System.Diagnostics;
using System.Text.RegularExpressions;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>
/// Smoke test that compiles the AotCompile fixture project with
/// <c>PublishAot=true</c> and <c>IsAotCompatible=true</c> and asserts that
/// no <c>IL2026</c> (RequiresUnreferencedCode) or <c>IL3050</c> (RequiresDynamicCode)
/// warnings are attributed to code emitted by the source generator.
/// This is the regression net for trim/AOT correctness.
/// </summary>
[Trait("Category", "AotSmoke")]
public class AotCleanCompileTests
{
    [Fact]
    public void Build_AotFixtureProject_GeneratorEmittedCodeRaisesNoAotTrimWarnings()
    {
        // Arrange
        // AppContext.BaseDirectory is .../tests/Filtering.Net.Generator.Tests/bin/<config>/net9.0/
        // Three levels up reaches .../tests/Filtering.Net.Generator.Tests/
        var fixtureDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "AotCompile"));

        Directory.Exists(fixtureDirectory).Should().BeTrue(
            $"Fixture directory not found at {fixtureDirectory} — was the project deployed alongside test assets? (BaseDirectory={AppContext.BaseDirectory})");

        var outputLines = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var errorLines = new System.Collections.Concurrent.ConcurrentQueue<string>();

        var startInfo = new ProcessStartInfo("dotnet", "build -c Release --nologo")
        {
            WorkingDirectory = fixtureDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var buildProcess = Process.Start(startInfo)!;
        buildProcess.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null) outputLines.Enqueue(eventArgs.Data);
        };
        buildProcess.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null) errorLines.Enqueue(eventArgs.Data);
        };
        buildProcess.BeginOutputReadLine();
        buildProcess.BeginErrorReadLine();

        // Act
        var exited = buildProcess.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);

        // Assert
        if (!exited)
        {
            buildProcess.Kill(entireProcessTree: true);
            buildProcess.WaitForExit(); // ensure async readers drain after kill
            false.Should().BeTrue("dotnet build should complete within 5 minutes");
        }

        buildProcess.WaitForExit(); // ensure async readers fully drain even on success

        var combinedOutput = string.Join(Environment.NewLine, outputLines) + Environment.NewLine
                           + string.Join(Environment.NewLine, errorLines);

        // Build output lines for generator-attributed warnings look like:
        //   .../Filtering.Net.Generator.FilterGenerator/AotFixture.UserFilter.g.cs(19,20): error IL2026: ...
        // The generator hint string appears in the file path BEFORE the error code on the same line.
        // We match lines containing the hint and either error code to confirm they are from generator-emitted code.
        const string generatorHint = "Filtering.Net.Generator.FilterGenerator";
        var generatorAttributedAotWarnings = Regex.Matches(
            combinedOutput,
            Regex.Escape(generatorHint) + @".*(IL2026|IL3050)");

        generatorAttributedAotWarnings.Should().BeEmpty(
            $"Generator-emitted code raised AOT trim warnings — the parameterless constructor " +
            $"emitted by the generator calls DefaultJsonTypeInfoResolver() which is attributed " +
            $"[RequiresUnreferencedCode] and [RequiresDynamicCode]. The generator must annotate " +
            $"that constructor with [RequiresUnreferencedCode]/[RequiresDynamicCode] or switch " +
            $"the parameterless constructor to an AOT-safe alternative.\n\n" +
            $"Matched warnings:\n{string.Join("\n", generatorAttributedAotWarnings.Select(m => m.Value))}\n\n" +
            $"--- full build output ---\n{combinedOutput}");

        buildProcess.ExitCode.Should().Be(0,
            $"dotnet build of the AOT fixture should exit 0 (no errors).\n\n" +
            $"--- full build output ---\n{combinedOutput}");
    }
}
