namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot + compile tests for [ConvertWith&lt;TConverter&gt;]: TModel from
/// the converter's <c>ValueConverter&lt;TModel, TProvider&gt;</c> base flows through to leaf
/// method value parameters and JSON deserialization, and EF translates the converter on its own.</summary>
public class ValueConverterEmissionTests
{
    // Stand-in for the EF Core ValueConverter<TModel, TProvider> base declared inline so the
    // generator test project doesn't take an EF Core dependency. The analyzer matches by
    // display string, so any class with this exact namespace + name resolves correctly.
    private const string ConsumerSource = """
        using Filtering.Net;
        namespace Microsoft.EntityFrameworkCore.Storage.ValueConversion
        {
            public abstract class ValueConverter<TModel, TProvider> { }
        }
        namespace Sample
        {
            using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
            public enum UserStatus { Active, Suspended }
            public sealed class UserStatusConverter : ValueConverter<UserStatus, string> { }
            public class User { public UserStatus Status { get; set; } }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Status))]
                [ConvertWith<UserStatusConverter>]
                private static partial void MapStatus();
            }
        }
        """;

    [Fact]
    public async Task EnumStoredAsString_UsesEnumForValueParameter()
    {
        // Arrange
        var driver = GeneratorRunner.RunDriver(ConsumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public void EnumStoredAsString_Compiles()
    {
        // Arrange
        // (source is declared as ConsumerSource above)

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(ConsumerSource);
    }
}
