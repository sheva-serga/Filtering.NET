namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>
/// Snapshot + compile tests for <c>[PageSettings]</c>. The attribute's values reach the runtime as
/// the <c>FilterSettings</c> literal in <c>CreateSchema</c>, and they differ from the generator's
/// fallbacks here so the snapshot can actually fail when the attribute stops being read. The
/// resulting behaviour is pinned separately in <see cref="EndToEndRuntimeTests"/>.
/// </summary>
public class ValidatePageEmissionTests
{
    private const string PageSettingsConsumerSource = """
        using Filtering.Net;
        namespace Sample;
        public class User { public string Name { get; set; } = ""; }
        [GenerateFilter<User>]
        [PageSettings(MaxPageSize = 100, DefaultPageSize = 25)]
        [Map(nameof(User.Name))]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public async Task PageSettingsApplied_EmitsConfiguredFilterSettings()
    {
        // Arrange
        var driver = GeneratorRunner.RunDriver(PageSettingsConsumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public void PageSettingsApplied_Compiles()
    {
        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(PageSettingsConsumerSource);
    }
}
