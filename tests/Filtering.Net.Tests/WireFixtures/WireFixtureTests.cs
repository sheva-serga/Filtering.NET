using System.Net;
using System.Text.Json;

using AwesomeAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Filtering.Net.Tests.WireFixtures;

public sealed class WireFixtureTests : IAsyncLifetime
{
    internal static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "wire-fixtures");

    private WebApplication? _echoServer;

    public static TheoryData<string> FixtureNames()
    {
        var fixtureNames = new TheoryData<string>();
        foreach (var fixturePath in Directory.GetFiles(FixtureDirectory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            fixtureNames.Add(Path.GetFileNameWithoutExtension(fixturePath));
        }
        return fixtureNames;
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(WireFixtureTests).Assembly);
        var echoServer = builder.Build();
        echoServer.MapGet("/minimal", ([AsParameters] FilterQuery query) => Results.Json(query.ToRequest(), WebOptions));
        echoServer.MapControllers();
        await echoServer.StartAsync(TestContext.Current.CancellationToken);
        _echoServer = echoServer;
    }

    public async ValueTask DisposeAsync()
    {
        if (_echoServer is not null) await _echoServer.DisposeAsync();
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
        var client = _echoServer!.GetTestClient();

        // Act
        var response = await client.GetAsync("/minimal?" + fixture.QueryString, TestContext.Current.CancellationToken);

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
        var client = _echoServer!.GetTestClient();

        // Act
        var response = await client.GetAsync("/mvc?" + fixture.QueryString, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Canonical(await ReadRequestAsync(response)).Should().Be(Canonical(fixture.Request));
    }

    [Theory]
    [InlineData("where=5")]
    [InlineData("where=%7B%22field%22%3A%22a%22%7D")]
    [InlineData("sort=name%3Aup")]
    [InlineData("page=abc")]
    public async Task MinimalApiBinding_MalformedParameter_ReturnsBadRequest(string queryString)
    {
        // Arrange
        var client = _echoServer!.GetTestClient();

        // Act
        var response = await client.GetAsync("/minimal?" + queryString, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("where=5")]
    [InlineData("where=%7B%22field%22%3A%22a%22%7D")]
    [InlineData("sort=name%3Aup")]
    [InlineData("page=abc")]
    public async Task MvcBinding_MalformedParameter_ReturnsBadRequest(string queryString)
    {
        // Arrange
        var client = _echoServer!.GetTestClient();

        // Act
        var response = await client.GetAsync("/mvc?" + queryString, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string Canonical(FilterRequest filterRequest) => JsonSerializer.Serialize(filterRequest, WebOptions);

    private static async Task<FilterRequest> ReadRequestAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<FilterRequest>(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            WebOptions)!;
}

internal sealed record WireFixture(string Name, FilterRequest Request, string QueryString)
{
    public static WireFixture Load(string fixtureDirectory, string fixtureName) =>
        JsonSerializer.Deserialize<WireFixture>(
            File.ReadAllText(Path.Combine(fixtureDirectory, fixtureName + ".json")),
            WireFixtureTests.WebOptions)!;
}

[ApiController]
[Route("mvc")]
public sealed class QueryStringEchoController : ControllerBase
{
    [HttpGet]
    public IActionResult Echo([FromQuery] FilterQuery query) => new JsonResult(query.ToRequest(), WireFixtureTests.WebOptions);
}
