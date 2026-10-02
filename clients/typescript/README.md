# filtering-net

[![npm](https://img.shields.io/npm/v/filtering-net.svg?logo=npm)](https://www.npmjs.com/package/filtering-net)
[![CI](https://github.com/sheva-serga/Filtering.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/sheva-serga/Filtering.NET/actions/workflows/ci.yml)
[![Docs](https://img.shields.io/badge/docs-Filtering.NET-blue.svg)](https://sheva-serga.github.io/Filtering.NET/guides/typescript-client/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/sheva-serga/Filtering.NET/blob/main/LICENSE)

Build [Filtering.NET](https://github.com/sheva-serga/Filtering.NET) filter, sort and paging requests in TypeScript.

- **Small and typed.** Plain functions that return the exact JSON the .NET `FilterRequest` reads, with full type definitions.
- **Zero dependencies.** ESM only, ES2020.
- **Query strings included.** `toQueryString` / `fromQueryString` speak the format ASP.NET binds with `FilterQuery`.
- **Released in lockstep** with the Filtering.Net NuGet packages and published with npm provenance.

```sh
npm install filtering-net
```

## Quick start

```ts
import { and, desc, field, or, request } from 'filtering-net';

const body = request({
  where: and(
    field('status').in(['Completed', 'Failed']),
    field('createdAt').gte(new Date('2026-01-01')),
    or(field('owner.name').contains('ali'), field('owner').isNull()),
  ),
  sort: [desc('createdAt')],
  page: 1,
  pageSize: 20,
});

await fetch('/api/orders/search', {
  method: 'POST',
  headers: { 'content-type': 'application/json' },
  body: JSON.stringify(body),
});
```

## Filters

`field(name)` starts a condition. Each operator returns one leaf:

| Call | Produces |
|------|----------|
| `field('age').eq(30)` / `.ne(30)` | `{ "field": "age", "op": "eq", "value": 30 }` |
| `.gt(v)` / `.gte(v)` / `.lt(v)` / `.lte(v)` | comparisons |
| `.contains('ali')` / `.startsWith('a')` / `.endsWith('z')` | text matching |
| `.in(['a', 'b'])` | one of several values |
| `.isNull()` | `{ "field": "age", "op": "isNull" }` (no value) |
| `.op('ilike', 'an%')` / `.op('isEmpty')` | a custom operator, with or without a value |

- A `Date` becomes its ISO string. `NaN` and `±Infinity` throw a `TypeError` instead of silently turning into `null`.
- Field and operator names pass through as written; the server matches both case-insensitively.

### Groups

`and(...)`, `or(...)` and `not(child)` combine conditions. `false`, `null`, `undefined` and `''` children are skipped, so optional conditions fit inline:

```ts
const where = and(
  field('isActive').eq(true),
  search && field('name').contains(search),       // dropped when search is ''
  onlyMine && field('ownerId').eq(currentUserId), // dropped when onlyMine is false
);
```

A group left with no children returns `undefined` and disappears from its parent, because the server rejects empty groups.

## Sorting and paging

| Call | Produces |
|------|----------|
| `asc('name')` / `desc('name')` | `{ "field": "name", "dir": "asc" }` / `"desc"` |
| `sortBy('name')` | `{ "field": "name" }`: the server's default direction for that field |
| `withTiebreakers(sort, desc('id'))` | `sort`, plus each tiebreaker whose field is not already sorted |

`request({ where, sort, page, pageSize })` assembles the body and leaves out anything `undefined`, as well as an empty `sort`.

## Query strings

```ts
import { fromQueryString, toQueryString } from 'filtering-net';

const query = toQueryString(body);
// where=<URL-encoded JSON>&sort=createdAt%3Adesc&page=1&pageSize=20

const restored = fromQueryString(location.search); // a string or URLSearchParams
```

`fromQueryString` reads `where`, `sort`, `page` and `pageSize` case-insensitively, as ASP.NET does, and ignores every other parameter. It throws a `FilterQueryStringError` whose `parameter` names the culprit when:

| Parameter | Rejected value |
|-----------|----------------|
| `where` | not JSON, or not a JSON object |
| `sort` | no field name, or a `:` suffix other than `asc` / `desc` |
| `page`, `pageSize` | anything `int.TryParse` would reject, including values outside the 32-bit range |
| `where`, `page`, `pageSize` | present more than once |

Nothing else is validated on the client: the server's `Validate` is the authority.

On the server, bind the query with `FilterQuery` and call `ToRequest()`:

```csharp
app.MapGet("/api/orders", async (
    [AsParameters] FilterQuery query,
    IFilterDefinition<Order> orderFilter,
    AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await dbContext.Orders.ApplyPagedAsync(orderFilter, query.ToRequest(), cancellationToken));
    }
    catch (FilterValidationException invalid)
    {
        return Results.BadRequest(invalid.Result);
    }
});
```

Use `[FromQuery] FilterQuery` on MVC controllers. A few behaviours are worth knowing before you rely on query strings:

- `[AsParameters]` reads `sort` from the query only on GET, HEAD and DELETE routes. On POST, PUT, PATCH and `Map`, minimal APIs read the `SortItem` array from the body.
- Minimal APIs and `[ApiController]` answer a value that fails to parse with a 400. Other MVC controllers must check `ModelState.IsValid`. MVC binds an empty `sort=` as an empty item, and `Validate` reports it.
- In Development, `RouteHandlerOptions.ThrowOnBadRequest` defaults to `true`, so behind `UseExceptionHandler` that 400 becomes a 500. Set `ThrowOnBadRequest = false` or map `BadHttpRequestException.StatusCode`.
- A sort field that itself contains `:` must carry an explicit direction: `meta:key:desc` round-trips, a bare `meta:key` does not.
- URLs have length limits (8 KB on Kestrel, 2 KB by default on IIS). Send large trees in a POST body.

## Versioning

`filtering-net@X.Y.Z` always matches the Filtering.Net `X.Y.Z` NuGet packages. Release notes are in the [changelog](https://github.com/sheva-serga/Filtering.NET/blob/main/CHANGELOG.md).

## Links

- [TypeScript client guide](https://sheva-serga.github.io/Filtering.NET/guides/typescript-client/)
- [Query-string binding guide](https://sheva-serga.github.io/Filtering.NET/guides/query-string-binding/)
- [The `FilterRequest` JSON shape](https://sheva-serga.github.io/Filtering.NET/concepts/filter-request-shape/)
- [Filtering.NET on GitHub](https://github.com/sheva-serga/Filtering.NET)

## License

[MIT](https://github.com/sheva-serga/Filtering.NET/blob/main/LICENSE)
