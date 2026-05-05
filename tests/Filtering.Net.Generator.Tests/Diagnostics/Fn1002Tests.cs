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
            public partial class OrderFilter
            {
                [Map(nameof(Order.CreatedAt))]
                private static partial void MapCreatedAt();
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
            public partial class UserFilter
            {
                [Map(nameof(User.Age))]
                private static partial void MapAge();
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
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
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
            public partial class UserFilter
            {
                [Map(nameof(User.Age), Sortable = true)]
                private static partial void MapAge();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1002");
    }
}
