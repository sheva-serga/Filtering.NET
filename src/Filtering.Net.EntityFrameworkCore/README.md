# Filtering.Net.EntityFrameworkCore

EF Core async helpers for [Filtering.Net](https://www.nuget.org/packages/Filtering.Net/). Adds `IQueryable<T>.ApplyPagedAsync(...)` and `PageResult<T>`.

Targets `net8.0` and `net9.0`.

## What it solves

`Filtering.Net` itself stays `netstandard2.0` and avoids any EF Core dependency, so it can load inside the analyzer process and on every consumer TFM. EF-specific async sequencing (`CountAsync` + `ToListAsync` + `PageResult<T>` packaging) lives in this package — install it when your call site is an EF Core controller / handler that wants a one-call paged response.

## Install

```sh
dotnet add package Filtering.Net
dotnet add package Filtering.Net.Generator
dotnet add package Filtering.Net.EntityFrameworkCore
```

The first two are required (runtime + generator). This package adds the EF-specific helpers on top.

## Quickstart

Given a `[GenerateFilter<User>]` partial (see [`Filtering.Net.Generator`](https://www.nuget.org/packages/Filtering.Net.Generator/)) and an EF `DbContext`:

```csharp
[ApiController]
[Route("users")]
public sealed class UsersController : ControllerBase
{
    [HttpPost("search")]
    public async Task<ActionResult<PageResult<User>>> Search(
        [FromBody] FilterRequest request,
        [FromServices] IFilterDefinition<User> userFilter,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            PageResult<User> page = await dbContext.Users
                .ApplyPagedAsync(userFilter, request, cancellationToken);
            return Ok(page);
        }
        catch (FilterValidationException invalid)
        {
            return BadRequest(invalid.Result);
        }
    }
}
```

`ApplyPagedAsync` validates the request, applies filter + sort, runs `CountAsync` + `ToListAsync` against the EF provider, and packages the results into a `PageResult<T>`.

## Key types

- **`PageResult<T>`** — `Items`, `TotalCount`, `Page`, `PageSize`. Computed members: `TotalPages`, `HasPrevious`, `HasNext`. Don't store the computed members; they round-trip through the constructor.
- **`FilteringEntityFrameworkExtensions.ApplyPagedAsync`** — the one extension method this package adds: `IQueryable<T>.ApplyPagedAsync(IFilterDefinition<T>, FilterRequest, CancellationToken)`.

## EF version pinning

| Target framework | EF Core version |
|------------------|-----------------|
| `net8.0` | 8.0.0 (LTS floor) |
| `net9.0` | 9.0.0 (current) |

Bumping these is intentional — the integration tests run against both, so a floor bump is a contract-affecting change.

## See also

- [Documentation site](https://sheva-serga.github.io/Filtering.NET/) — full guides, API reference, diagnostics catalogue.
- [Repo on GitHub](https://github.com/sheva-serga/Filtering.NET) — source, issue tracker, contribution notes.
- [`Filtering.Net`](https://www.nuget.org/packages/Filtering.Net/) — runtime request types.
- [`Filtering.Net.Generator`](https://www.nuget.org/packages/Filtering.Net.Generator/) — source generator + 25-rule analyzer.

## License

MIT.
