using System.Net;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.WireFixtures;

public sealed class WireFixtureTests(EchoServerFixture echoServer) : IClassFixture<EchoServerFixture>
{
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "wire-fixtures");

    private readonly EchoServerFixture _echoServer = echoServer;

    public static TheoryData<string> FixtureNames()
    {
        var fixtureNames = new TheoryData<string>();
        foreach (var fixturePath in Directory.GetFiles(FixtureDirectory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            fixtureNames.Add(Path.GetFileNameWithoutExtension(fixturePath));
        }
        return fixtureNames;
    }

    [Fact]
    public void FixtureDirectory_ContainsTheSharedFixtures_IsNotEmpty()
    {
        // Act
        var fixtureNames = FixtureNames();

        // Assert
        fixtureNames.Should().HaveCount(8);
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Validate_FixtureRequest_PassesAgainstTheSampleDefinition(string fixtureName)
    {
        // Arrange
        var fixture = WireFixture.Load(FixtureDirectory, fixtureName);
        var definition = WireSampleDefinition.Create();

        // Act
        var validationResult = definition.Validate(fixture.Request);

        // Assert
        validationResult.IsValid.Should().BeTrue(
            string.Join("; ", validationResult.Errors.Select(error => $"{error.Path}: {error.Message}")));
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public async Task MinimalApiBinding_FixtureQueryString_ProducesTheFixtureRequest(string fixtureName)
    {
        // Arrange
        var fixture = WireFixture.Load(FixtureDirectory, fixtureName);

        // Act
        var response = await _echoServer.Client.GetAsync("/minimal?" + fixture.QueryString, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Canonical(await ReadRequestAsync(response)).Should().Be(Canonical(fixture.Request));
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public async Task MvcBinding_FixtureQueryString_ProducesTheFixtureRequest(string fixtureName)
    {
        // Arrange
        var fixture = WireFixture.Load(FixtureDirectory, fixtureName);

        // Act
        var response = await _echoServer.Client.GetAsync("/mvc?" + fixture.QueryString, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Canonical(await ReadRequestAsync(response)).Should().Be(Canonical(fixture.Request));
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public async Task PlainMvcBinding_FixtureQueryString_ProducesTheFixtureRequestWithValidModelState(string fixtureName)
    {
        // Arrange
        var fixture = WireFixture.Load(FixtureDirectory, fixtureName);

        // Act
        var echo = await ReadPlainMvcEchoAsync("/plain?" + fixture.QueryString);

        // Assert
        echo.ModelStateValid.Should().BeTrue();
        Canonical(echo.Request).Should().Be(Canonical(fixture.Request));
    }

    [Theory]
    [InlineData("where=5", "Where")]
    [InlineData("where=%7B%22field%22%3A%22a%22%7D", "Where")]
    [InlineData("where=%7B%7D", "Where")]
    [InlineData("sort=name%3Aup", "Sort")]
    [InlineData("sort=%3Adesc", "Sort")]
    [InlineData("page=abc", "Page")]
    public async Task PlainMvcBinding_MalformedParameter_InvalidatesModelStateAndLeavesThePartUnbound(string queryString, string expectedErrorKey)
    {
        // Act
        var echo = await ReadPlainMvcEchoAsync("/plain?" + queryString);

        // Assert
        echo.ModelStateValid.Should().BeFalse();
        echo.ErrorKeys.Should().Equal(expectedErrorKey);
        Canonical(echo.Request).Should().Be(Canonical(new FilterRequest()));
    }

    [Fact]
    public async Task PlainMvcBinding_EmptySortValue_KeepsModelStateValidAndBindsAnItemThatFailsSortValidation()
    {
        // Arrange
        var definition = WireSampleDefinition.Create();

        // Act
        var echo = await ReadPlainMvcEchoAsync("/plain?sort=");
        var validationResult = definition.Validate(echo.Request);

        // Assert
        echo.ModelStateValid.Should().BeTrue();
        echo.Request.Sort.Should().ContainSingle().Which.Should().BeNull();
        validationResult.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FilterValidationError(
                "sort[0].field", FilterValidationCode.NotSortable, "A sort item must name a field."));
    }

    [Theory]
    [InlineData("where=5")]
    [InlineData("where=%7B%22field%22%3A%22a%22%7D")]
    [InlineData("where=%7B%7D")]
    [InlineData("sort=name%3Aup")]
    [InlineData("sort=%3Adesc")]
    [InlineData("sort=")]
    [InlineData("sort=name&sort=")]
    [InlineData("page=abc")]
    public async Task MinimalApiBinding_MalformedParameter_ReturnsBadRequest(string queryString)
    {
        // Act
        var response = await _echoServer.Client.GetAsync("/minimal?" + queryString, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("where=5")]
    [InlineData("where=%7B%22field%22%3A%22a%22%7D")]
    [InlineData("where=%7B%7D")]
    [InlineData("sort=name%3Aup")]
    [InlineData("sort=%3Adesc")]
    [InlineData("page=abc")]
    public async Task MvcBinding_MalformedParameter_ReturnsBadRequest(string queryString)
    {
        // Act
        var response = await _echoServer.Client.GetAsync("/mvc?" + queryString, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("sort=", "sort[0].field")]
    [InlineData("sort=name&sort=", "sort[1].field")]
    public async Task MvcBinding_EmptySortValue_BindsAnItemThatFailsSortValidation(string queryString, string expectedErrorPath)
    {
        // Arrange
        var definition = WireSampleDefinition.Create();

        // Act
        var response = await _echoServer.Client.GetAsync("/mvc?" + queryString, TestContext.Current.CancellationToken);
        var validationResult = definition.Validate(await ReadRequestAsync(response));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        validationResult.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FilterValidationError(
                expectedErrorPath, FilterValidationCode.NotSortable, "A sort item must name a field."));
    }

    private static string Canonical(FilterRequest filterRequest) => JsonSerializer.Serialize(filterRequest, EchoServerFixture.WebOptions);

    private static async Task<FilterRequest> ReadRequestAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<FilterRequest>(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            EchoServerFixture.WebOptions)!;

    private async Task<PlainMvcEcho> ReadPlainMvcEchoAsync(string requestUri)
    {
        var response = await _echoServer.Client.GetAsync(requestUri, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<PlainMvcEcho>(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            EchoServerFixture.WebOptions)!;
    }
}

internal sealed record WireFixture(string Name, FilterRequest Request, string QueryString)
{
    public static WireFixture Load(string fixtureDirectory, string fixtureName) =>
        JsonSerializer.Deserialize<WireFixture>(
            File.ReadAllText(Path.Combine(fixtureDirectory, fixtureName + ".json")),
            EchoServerFixture.WebOptions)!;
}
