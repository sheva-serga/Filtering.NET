namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0006Tests
{
    [Fact]
    public void CustomTypePropertyWithoutExplicitProfile_FiresFN0006()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Money { public decimal Amount { get; set; } public string Currency { get; set; } = ""; }
            public class Order { public Money Total { get; set; } = new(); }
            [GenerateFilter<Order>]
            [Map(nameof(Order.Total))]
            public partial class OrderFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0006");
    }

    [Fact]
    public void StringPropertyWithoutExplicitProfile_DoesNotFireFN0006()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0006");
    }

    [Fact]
    public void NoInferableProfile_ReportsEntityPropertyAsAdditionalLocation()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Money { public decimal Amount { get; set; } public string Currency { get; set; } = ""; }
            public class Order { public Money Total { get; set; } = new(); }
            [GenerateFilter<Order>]
            [Map(nameof(Order.Total))]
            public partial class OrderFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0006", expectedAdditionalCount: 1);
    }
}
