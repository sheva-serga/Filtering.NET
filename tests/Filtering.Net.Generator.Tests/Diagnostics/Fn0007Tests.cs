namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0007Tests
{
    [Fact]
    public void CustomTypePropertyWithoutExplicitProfile_FiresFN0007()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Money { public decimal Amount { get; set; } public string Currency { get; set; } = ""; }
            public class Order { public Money Total { get; set; } = new(); }
            [GenerateFilter<Order>]
            public partial class OrderFilter
            {
                [Map(nameof(Order.Total))]
                private static partial void MapTotal();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0007");
    }

    [Fact]
    public void StringPropertyWithoutExplicitProfile_DoesNotFireFN0007()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0007");
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
            public partial class OrderFilter
            {
                [Map(nameof(Order.Total))]
                private static partial void MapTotal();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0007", expectedAdditionalCount: 1);
    }
}
