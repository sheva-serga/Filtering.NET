namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0021Tests
{
    [Fact]
    public void FilterClassWithBaseClass_FiresFN0021()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            public abstract class FilterBase { }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter : FilterBase
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0021");
    }

    [Fact]
    public void FilterClassWithInterfaceOnly_DoesNotFireFN0021()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            public interface IMarker { }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter : IMarker
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0021");
    }
}
