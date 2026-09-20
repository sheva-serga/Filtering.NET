namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1002 (NotSortableLikelyOmission): numeric/date property mapped without Sortable=true.</summary>
public class Fn1002Tests
{
    [Fact]
    public void DateTimePropertyNotSortable_FiresFN1002()
    {
        // Arrange
        var source = """
            using System;
            using Filtering.Net;
            namespace TestNs;
            public class Order { public DateTime CreatedAt { get; set; } }
            [GenerateFilter<Order>]
            [Map(nameof(Order.CreatedAt))]
            public partial class OrderFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1002");
    }

    [Fact]
    public void IntPropertyNotSortable_FiresFN1002()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public int Age { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Age))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1002");
    }

    [Fact]
    public void StringPropertyNotSortable_DoesNotFireFN1002()
    {
        // Arrange
        // Strings aren't on the heuristic list — only numeric/date types are.
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
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1002");
    }

    [Fact]
    public void IntPropertyExplicitlySortable_DoesNotFireFN1002()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public int Age { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Age), Sortable = true)]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1002");
    }
}
