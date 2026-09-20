namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0022Tests
{
    [Fact]
    public void FilterClassWithBaseClass_FiresFN0022()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            public abstract class FilterBase { }
            [GenerateFilter<User>]
            public partial class UserFilter : FilterBase
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0022");
    }

    [Fact]
    public void FilterClassWithInterfaceOnly_DoesNotFireFN0022()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            public interface IMarker { }
            [GenerateFilter<User>]
            public partial class UserFilter : IMarker
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0022");
    }
}
