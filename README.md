# Filtering.Net

[![CI](https://github.com/sheva-serga/Filtering.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/sheva-serga/Filtering.NET/actions/workflows/ci.yml)
[![NuGet — Filtering.Net](https://img.shields.io/nuget/v/Filtering.Net.svg?label=Filtering.Net)](https://www.nuget.org/packages/Filtering.Net/)
[![NuGet — Filtering.Net.Generator](https://img.shields.io/nuget/v/Filtering.Net.Generator.svg?label=Filtering.Net.Generator)](https://www.nuget.org/packages/Filtering.Net.Generator/)
[![NuGet — Filtering.Net.EntityFrameworkCore](https://img.shields.io/nuget/v/Filtering.Net.EntityFrameworkCore.svg?label=Filtering.Net.EntityFrameworkCore)](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/)
[![npm — filtering-net](https://img.shields.io/npm/v/filtering-net.svg?label=filtering-net&logo=npm)](https://www.npmjs.com/package/filtering-net)
[![Docs](https://img.shields.io/badge/docs-sheva--serga.github.io-blue.svg)](https://sheva-serga.github.io/Filtering.NET/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Type-safe filtering, sorting and paging for `IQueryable<T>` and EF Core. You declare which fields clients may filter and sort on; a source generator turns that into a typed schema, and one runtime engine validates every request before translating it to SQL. A dependency-free TypeScript client builds the same requests in the browser.

**[Documentation](https://sheva-serga.github.io/Filtering.NET/)** · **[TypeScript client on npm](https://www.npmjs.com/package/filtering-net)** · **[Changelog](CHANGELOG.md)** · **[Contributing](CONTRIBUTING.md)**

## Why Filtering.Net

- **Structured requests, not a string DSL.** Clients send a `FilterRequest` JSON tree (`and` / `or` / `not` groups of `field` / `op` / `value` leaves), or the same request as a query string. There is no expression parser to escape around.
- **You decide the surface.** Only mapped fields can be filtered or sorted, each with the operators its type allows. Aliases, computed `[PropertyMap]` rules, custom profiles and `[MapNested]` filters for navigation properties cover the rest.
- **Validated before it touches the database.** Every request is checked against the generated schema first. Bad input becomes a structured list of `FilterValidationError`s (path, code, message) that you return as a 400.
- **Mistakes caught at compile time.** A 37-rule analyzer (`FN0001`–`FN0029` errors, `FN1001`–`FN1008` warnings) flags mapping conflicts and EF Core translation problems while you type.
- **Trim and Native AOT friendly.** The generated code uses no reflection over your types and never calls `Compile()`. The runtime is annotated `IsAotCompatible` for `net8.0` and later.
- **One version everywhere.** The NuGet packages and the `filtering-net` npm client ship together under the same version number.

## Install

```sh
dotnet add package Filtering.Net
dotnet add package Filtering.Net.Generator
dotnet add package Filtering.Net.EntityFrameworkCore   # ApplyPagedAsync for EF Core
```

```sh
npm install filtering-net                              # optional TypeScript client
```

## Quick start

**1. Describe what may be filtered.** Put one `[Map]` per field on a partial class; add a `[PropertyMap]` rule for anything computed.

```csharp
public sealed class User
{
    public int    Id        { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName  { get; set; } = "";
    public int    Age       { get; set; }
    public bool   IsActive  { get; set; }
}

[GenerateFilter<User>]
[Map(nameof(User.Id),        Sortable = true)]
[Map(nameof(User.FirstName), Sortable = true, Alias = "name")]
[Map(nameof(User.Age),       Sortable = true)]
[Map(nameof(User.IsActive))]
public partial class UserFilter
{
    [PropertyMap("FullName", Sortable = true)]
    private static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
        builder.For(user => user.FirstName + " " + user.LastName)
               .Operator<string>("contains", (fullName, value) => fullName.Contains(value));
}
```

The generator makes `UserFilter` a `FilterDefinition<User>` (the engine behind `IFilterDefinition<User>`) and emits a DI extension.

**2. Register it.**

```csharp
builder.Services.AddFiltering(); // emitted by the generator
```

**3. Apply a request.**

```csharp
app.MapPost("/api/users/search", async (
    FilterRequest request,
    IFilterDefinition<User> userFilter,
    AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await dbContext.Users.ApplyPagedAsync(userFilter, request, cancellationToken));
    }
    catch (FilterValidationException invalid)
    {
        return Results.BadRequest(invalid.Result);
    }
});
```

A request body looks like this:

```json
{
  "where": {
    "and": [
      { "field": "fullName", "op": "contains", "value": "ali" },
      { "field": "isActive", "op": "eq", "value": true }
    ]
  },
  "sort": [{ "field": "age", "dir": "desc" }],
  "page": 1,
  "pageSize": 25
}
```

Field and operator names match case-insensitively. `ApplyPagedAsync` validates, filters, sorts and pages in one call and returns a `PageResult<User>` with the items and the total count.

## Query strings

The same request can travel in a URL: `?where=<JSON>&sort=age:desc&page=1&pageSize=25`. Bind it with `[AsParameters] FilterQuery` on minimal APIs or `[FromQuery] FilterQuery` on MVC:

```csharp
app.MapGet("/api/users", async (
    [AsParameters] FilterQuery query,
    IFilterDefinition<User> userFilter,
    AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await dbContext.Users.ApplyPagedAsync(userFilter, query.ToRequest(), cancellationToken));
    }
    catch (FilterValidationException invalid)
    {
        return Results.BadRequest(invalid.Result);
    }
});
```

Read the [query-string binding guide](https://sheva-serga.github.io/Filtering.NET/guides/query-string-binding/) before relying on it: it covers which HTTP methods bind `sort` from the query, the Development-environment 500 trap and URL length limits.

## TypeScript client

[`filtering-net`](https://www.npmjs.com/package/filtering-net) builds the exact JSON (or query string) the server reads, with no runtime dependencies:

```ts
import { and, desc, field, request, toQueryString } from 'filtering-net';

const body = request({
  where: and(field('fullName').contains('ali'), field('isActive').eq(true)),
  sort: [desc('age')],
  page: 1,
  pageSize: 25,
});

await fetch('/api/users/search', { method: 'POST', body: JSON.stringify(body), headers: { 'content-type': 'application/json' } });
await fetch(`/api/users?${toQueryString(body)}`);
```

See the [client README](clients/typescript/README.md) and the [TypeScript client guide](https://sheva-serga.github.io/Filtering.NET/guides/typescript-client/).

## Packages

| Package | Targets | What's in it |
|---------|---------|--------------|
| [`Filtering.Net`](https://www.nuget.org/packages/Filtering.Net/) | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` | Request types (`FilterRequest`, `FilterNode`, `SortItem`, `FilterQuery`), the attributes (`[GenerateFilter<T>]`, `[Map]`, `[MapNested]`, `[PropertyMap]`, `[FilterProfile<T>]`, …), built-in profiles, the `FilterDefinition<T>` engine and the synchronous `Apply` extension. `DateOnlyFilter` / `TimeOnlyFilter` need `net8.0`+. |
| [`Filtering.Net.Generator`](https://www.nuget.org/packages/Filtering.Net.Generator/) | `netstandard2.0` | Incremental source generator plus the 37-rule analyzer. Analyzer-only: nothing is added to your output. |
| [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) | `net8.0`, `net9.0`, `net10.0` | `ApplyPagedAsync` and `PageResult<T>` for EF Core. |
| [`filtering-net`](https://www.npmjs.com/package/filtering-net) (npm) | ES2020, ESM | TypeScript combinators (`field`, `and` / `or` / `not`, `asc` / `desc`, `request`) and `toQueryString` / `fromQueryString`. Published with npm provenance. |

## Learn more

- [Getting started](https://sheva-serga.github.io/Filtering.NET/getting-started/installation/): installation, your first filter, DI registration
- [The `FilterRequest` JSON shape](https://sheva-serga.github.io/Filtering.NET/concepts/filter-request-shape/) and [profiles and operators](https://sheva-serga.github.io/Filtering.NET/concepts/profiles-and-operators/)
- [Per-property overrides with `[PropertyMap]`](https://sheva-serga.github.io/Filtering.NET/guides/property-map-overrides/) and [nested filters](https://sheva-serga.github.io/Filtering.NET/guides/nested-filters/)
- [Handling validation errors](https://sheva-serga.github.io/Filtering.NET/guides/handling-validation-errors/) and [trim / AOT setup](https://sheva-serga.github.io/Filtering.NET/guides/aot-clean-setup/)
- [Diagnostics catalogue](https://sheva-serga.github.io/Filtering.NET/diagnostics/)
- [Sample app](samples/UserManagement.WebApi/README.md): ASP.NET Core 9 + EF Core 9 + PostgreSQL, with a `docker-compose.yml`

## Contributing

Issues and pull requests are welcome. [CONTRIBUTING.md](CONTRIBUTING.md) covers building, testing, the coding conventions and the release process.

## License

[MIT](LICENSE)
