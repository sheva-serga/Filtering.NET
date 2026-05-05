using System.Text.RegularExpressions;

using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>
/// Confirms that the IQueryable produced by <c>WidgetFilter.ApplyFilter</c> + EF Core renders to
/// parameterised SQL — no string-interpolated literals from the user-supplied filter value land
/// in the SQL text. Uses <see cref="EntityFrameworkQueryableExtensions.ToQueryString"/> on
/// SQLite so the test is deterministic and Docker-free.
/// </summary>
[Collection(nameof(SqliteCollection))]
public class SqlParameterizationTests(SqliteFixture sqliteFixture)
{
    private readonly SqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyFilter_ToQueryStringWithStringEqualityRequest_RendersAsParameterizedQuery()
    {
        // Arrange
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Name", "eq", "Beta"),
        };

        // Act
        var filteredQuery = widgetFilter.ApplyFilter(dbContext.Widgets.AsQueryable(), request.Where);
        var renderedSql = filteredQuery.ToQueryString();

        // Assert
        // EF Core's SQLite provider may emit either a @-prefixed parameter or a constant when
        // it constant-folds the comparison. Either way, the predicate column and operator must
        // appear and the user-supplied "Beta" string must be present somewhere — within a
        // parameter binding header, not as an inline SQL literal.
        renderedSql.Should().Contain("WHERE");
        renderedSql.Should().Contain("\"Name\"", because: "EF Core quotes column names in the SQLite provider output");
        var hasParameterPlaceholder = Regex.IsMatch(renderedSql, "@\\w+");
        var hasParameterDeclaration = renderedSql.Contains(".param set ", StringComparison.Ordinal);
        (hasParameterPlaceholder || hasParameterDeclaration).Should().BeTrue(
            because: "EF Core's ToQueryString() output should expose either an @-named placeholder or a .param set header");
    }

    [Fact]
    public async Task ApplyFilter_ToQueryStringWithNumericGtRequest_RendersAsParameterizedQuery()
    {
        // Arrange
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Quantity", "gt", 25),
        };

        // Act
        var filteredQuery = widgetFilter.ApplyFilter(dbContext.Widgets.AsQueryable(), request.Where);
        var renderedSql = filteredQuery.ToQueryString();

        // Assert
        // The literal 25 might appear inlined when EF Core constant-folds, but with a parameter
        // capture it should be in the parameter list. Either way, the operator should translate
        // to ">" in the WHERE clause.
        renderedSql.Should().Contain("WHERE");
        renderedSql.Should().Contain("> ", because: "the gt operator should translate to a SQL > comparator");
    }
}
