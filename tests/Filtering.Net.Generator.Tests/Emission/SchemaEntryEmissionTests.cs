namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>
/// One snapshot per distinct <c>CreateSchema</c> entry shape: a numeric property, a dotted
/// navigation accessor, an intercepted property, an aliased property, and two sortable properties.
/// The generator emits no per-concern code any more — filtering, sorting, validation and paging all
/// run in the generic engine over this schema — so the behaviour of those concerns is pinned by the
/// runtime tests in <see cref="EndToEndRuntimeTests"/>, not here.
/// </summary>
public class SchemaEntryEmissionTests
{
    [Fact]
    public async Task NumericProperty_EmitsNumericProfileSchemaEntry()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public int Age { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Age))]
            public partial class UserFilter
            {
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task NavigationPath_EmitsDottedAccessor()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<User>]
            [Map("Department.Name", Alias = "dept")]
            public partial class UserFilter
            {
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task Interceptor_EmitsInterceptCallOnSchemaEntry()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Email))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Email))]
                private static string InterceptEmail(InterceptContext context, string value)
                    => value.Trim().ToLowerInvariant();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task Alias_EmitsAliasOnSchemaEntry()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Alias = "displayName")]
            public partial class UserFilter
            {
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task TwoSortableProperties_EmitSortableSchemaEntries()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User
            {
                public string Name { get; set; } = "";
                public int Age { get; set; }
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Sortable = true)]
            [Map(nameof(User.Age), Sortable = true)]
            public partial class UserFilter
            {
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }
}
