using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

public class EnumProfileEmissionTests
{
    private const string CollidingEnumNamesSource = """
        using Filtering.Net;
        namespace Sample.Billing
        {
            public enum Status { Draft, Paid }
            public class Invoice { public Status Status { get; set; } }
            [GenerateFilter<Invoice>]
            [Map(nameof(Invoice.Status))]
            public partial class InvoiceFilter { }
        }
        namespace Sample.Shipping
        {
            public enum Status { Packed, Sent }
            public class Parcel { public Status Status { get; set; } }
            [GenerateFilter<Parcel>]
            [Map(nameof(Parcel.Status))]
            public partial class ParcelFilter { }
        }
        """;

    [Fact]
    public void TwoSameNamedEnumsInDifferentNamespaces_EmitDistinctProfileFiles()
    {
        // Arrange — a shared hint name throws inside AddSource and drops the generator's whole
        // source set, so every filter class in the compilation would disappear.
        var runResult = GeneratorRunner.RunDriver(CollidingEnumNamesSource, excludeDiAbstractions: false).GetRunResult();

        // Act
        var enumProfileFileNames = runResult.GeneratedTrees
            .Select(tree => Path.GetFileName(tree.FilePath))
            .Where(fileName => fileName.Contains("StatusFilter", StringComparison.Ordinal))
            .ToList();

        // Assert
        enumProfileFileNames.Should().HaveCount(2);
        enumProfileFileNames.Should().OnlyHaveUniqueItems();
        runResult.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "CS8785");
    }

    [Fact]
    public void TwoSameNamedEnumsInDifferentNamespaces_Compiles()
    {
        // Act
        // (no separate act step — AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(CollidingEnumNamesSource);
    }

    [Fact]
    public void EnumNestedInAnotherType_Compiles()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public static class Catalog
            {
                public enum Availability { InStock, BackOrdered }
            }
            public class Product { public Catalog.Availability Availability { get; set; } }
            [GenerateFilter<Product>]
            [Map(nameof(Product.Availability))]
            public partial class ProductFilter { }
            """;

        // Act
        // (no separate act step — AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public void EnumNestedInAnotherTypeCollidingWithATopLevelEnum_Compiles()
    {
        // Arrange — the simple names match, so both profiles must fall back to mangled full names.
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public enum Availability { InStock, BackOrdered }
            public static class Catalog
            {
                public enum Availability { Listed, Delisted }
            }
            public class Product { public Availability Availability { get; set; } }
            public class Listing { public Catalog.Availability Availability { get; set; } }
            [GenerateFilter<Product>]
            [Map(nameof(Product.Availability))]
            public partial class ProductFilter { }
            [GenerateFilter<Listing>]
            [Map(nameof(Listing.Availability))]
            public partial class ListingFilter { }
            """;

        // Act
        // (no separate act step — AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(consumerSource);
    }

    [Fact]
    public Task SingleEnum_EmitsAllOperators()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;

            public enum UserStatus { Active, Closed }
            public class User { public UserStatus Status { get; set; } }

            [GenerateFilter<User>]
            [Map(nameof(User.Status))]
            public partial class UserFilter
            {
            }
            """;
        var generatorRunResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false).GetRunResult();
        var enumProfileSource = generatorRunResult.GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith("UserStatusFilter.g.cs", StringComparison.Ordinal))
            .ToString();

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(enumProfileSource).UseDirectory("Snapshots");
    }
}
