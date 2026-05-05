using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.ModelExtraction;

public class TypedValueDetectionTests
{
    [Fact]
    public void ElementOnlyClass_HasNoTypedValuePropertyFlag()
    {
        // Arrange + Act
        var model = ExtractModel("""
            using Filtering.Net;
            namespace Sample;
            public sealed class User { public string Email { get; set; } = string.Empty; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map("Email")]
                private static partial void MapEmail();
            }
            """);

        // Assert
        model.HasAnyTypedValueProperty.Should().BeFalse();
        model.Properties.Should().AllSatisfy(p => p.HasTypedValueOperator.Should().BeFalse());
    }

    [Fact]
    public void ClassWithCustomOperator_FlagsTypedValueOnProperty()
    {
        // Arrange + Act
        var model = ExtractModel("""
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace Sample;
            public sealed record RegexFilterValue(string Pattern);
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringWithRegexProfile
            {
                [FilterOperator("regex")]
                public static Expression<Func<string, RegexFilterValue, bool>> Regex =>
                    (column, value) => System.Text.RegularExpressions.Regex.IsMatch(column, value.Pattern);
            }
            public sealed class User { public string Email { get; set; } = string.Empty; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map("Email", Profile = typeof(StringWithRegexProfile))]
                private static partial void MapEmail();
            }
            """);

        // Assert
        model.HasAnyTypedValueProperty.Should().BeTrue();
        var emailProperty = model.Properties.Single(p => p.PropertyName == "Email");
        emailProperty.HasTypedValueOperator.Should().BeTrue();
    }

    [Fact]
    public void ClassWithPropertyMapOverride_FlagsTypedValueOnOverride()
    {
        // Arrange + Act
        var model = ExtractModel("""
            using Filtering.Net;
            namespace Sample;
            public sealed class User { public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                public static FilterRule<User, string> FullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(u => u.FirstName + " " + u.LastName)
                           .Operator("eq", (string column, string value) => column == value);
            }
            """);

        // Assert
        model.HasAnyTypedValueProperty.Should().BeTrue();
        var fullNameOverride = model.Overrides.Single(o => o.PropertyName == "FullName");
        fullNameOverride.HasTypedValueOperator.Should().BeTrue();
    }

    [Fact]
    public void ClassWithUnaryCustomOperatorOnly_DoesNotFlagTypedValue()
    {
        // Arrange + Act
        var model = ExtractModel("""
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace Sample;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringWithEmptyProfile
            {
                [FilterOperator("isEmpty")]
                public static Expression<Func<string, bool>> IsEmpty => column => column.Length == 0;
            }
            public sealed class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map("Email", Profile = typeof(StringWithEmptyProfile))]
                private static partial void MapEmail();
            }
            """);

        // Assert — unary custom operator does NOT flag a typed-value operator because it has no value type
        model.HasAnyTypedValueProperty.Should().BeFalse();
        model.Properties.Should().AllSatisfy(p => p.HasTypedValueOperator.Should().BeFalse());
    }

    private static FilterClassModel ExtractModel(string source)
    {
        var models = GeneratorRunner.ExtractFilterClassModels(source, excludeDiAbstractions: false);
        return models.Should().ContainSingle(
            because: "the source should contain exactly one valid [GenerateFilter<>] class").Subject;
    }
}
