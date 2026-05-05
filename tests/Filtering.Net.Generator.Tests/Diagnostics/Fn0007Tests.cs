namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0007 (InvalidValueConverter): [ConvertWith&lt;T&gt;] T does not inherit from EF Core's ValueConverter&lt;,&gt;.</summary>
public class Fn0007Tests
{
    /// <summary>
    /// We declare a fake <c>Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter</c>
    /// in the test source so we don't need a real EF Core reference. The analyzer matches the
    /// open-generic display string, so this stand-in is sufficient.
    /// </summary>
    private const string FakeValueConverterDeclaration = """
        namespace Microsoft.EntityFrameworkCore.Storage.ValueConversion
        {
            public abstract class ValueConverter { }
            public class ValueConverter<TModel, TProvider> : ValueConverter { }
        }
        """;

    [Fact]
    public void ConvertWithReferencesUnrelatedType_FiresFN0007()
    {
        // Arrange
        // Note the converter is just `class NotAConverter`, with no relation to ValueConverter<,>.
        var source = $$"""
            using Filtering.Net;
            {{FakeValueConverterDeclaration}}
            namespace TestNs
            {
                public class NotAConverter { }
                public class User { public string Name { get; set; } = ""; }
                [GenerateFilter<User>]
                public partial class UserFilter
                {
                    [Map(nameof(User.Name))]
                    [ConvertWith<NotAConverter>]
                    private static partial void MapName();
                }
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0007");
    }

    [Fact]
    public void ConvertWithReferencesValueConverterSubclass_DoesNotFireFN0007()
    {
        // Arrange
        var source = $$"""
            using Filtering.Net;
            using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
            {{FakeValueConverterDeclaration}}
            namespace TestNs
            {
                public class StringToStringConverter : ValueConverter<string, string> { }
                public class User { public string Name { get; set; } = ""; }
                [GenerateFilter<User>]
                public partial class UserFilter
                {
                    [Map(nameof(User.Name))]
                    [ConvertWith<StringToStringConverter>]
                    private static partial void MapName();
                }
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0007");
    }
}
