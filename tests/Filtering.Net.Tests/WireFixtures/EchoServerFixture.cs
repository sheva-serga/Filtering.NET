using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Filtering.Net.Tests.WireFixtures;

/// <summary>
/// xUnit class fixture that starts one in-process echo host for the whole test class: a minimal
/// API route and an MVC controller that bind a <see cref="FilterQuery"/> from the query string and
/// return the resulting <see cref="FilterRequest"/>.
/// </summary>
public sealed class EchoServerFixture : IAsyncLifetime
{
    internal static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private WebApplication? _echoServer;
    private HttpClient? _client;

    public HttpClient Client => _client ?? throw new InvalidOperationException("The echo server has not been started.");

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers().AddApplicationPart(typeof(EchoServerFixture).Assembly);
        var echoServer = builder.Build();
        echoServer.MapGet("/minimal", ([AsParameters] FilterQuery query) => Results.Json(query.ToRequest(), WebOptions));
        echoServer.MapControllers();
        await echoServer.StartAsync(TestContext.Current.CancellationToken);
        _echoServer = echoServer;
        _client = echoServer.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_echoServer is not null) await _echoServer.DisposeAsync();
    }
}

[ApiController]
[Route("mvc")]
public sealed class QueryStringEchoController : ControllerBase
{
    [HttpGet]
    public IActionResult Echo([FromQuery] FilterQuery query) => new JsonResult(query.ToRequest(), EchoServerFixture.WebOptions);
}
