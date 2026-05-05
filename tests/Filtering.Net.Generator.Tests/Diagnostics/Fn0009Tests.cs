namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0009 (NoInferableProfile): property's CLR type has no built-in profile and no explicit Profile = is set.</summary>
public class Fn0009Tests
{
    [Fact]
    public void CustomTypePropertyWithoutExplicitProfile_FiresFN0009()
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
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0009");
    }

    [Fact]
    public void StringPropertyWithoutExplicitProfile_DoesNotFireFN0009()
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
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0009");
    }
}
