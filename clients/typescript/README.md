# filtering-net

Compose [Filtering.NET](https://sheva-serga.github.io/Filtering.NET/) filter, sort and paging requests from TypeScript. Zero dependencies, ESM only. Every function returns the plain JSON the .NET `FilterRequest` reads.

```sh
npm install filtering-net
```

## Compose a request

```ts
import { and, desc, field, not, or, request, withTiebreakers } from 'filtering-net';

const where = and(
  field('status').in(['Completed', 'Failed']),
  field('createDate').gt(start),            // Date values become ISO strings
  search && field('initiatorFullName').contains(search),
  or(field('isActive').eq(false), not(field('department.name').isNull())),
);

const body = request({ where, sort: withTiebreakers([desc('createDate')], desc('id')), page: 1, pageSize: 20 });
await fetch('/api/sessions/filter', { method: 'POST', body: JSON.stringify(body), headers: { 'content-type': 'application/json' } });
```

- `false`, `null`, `undefined` and `''` children are skipped, so optional conditions compose inline.
- An `and`/`or` with no remaining children returns `undefined` and disappears from its parent. The server rejects empty groups.
- Field and operator names pass through as written; the server matches both case-insensitively. Custom operators use `field('name').op('ilike', value)`, and unary ones omit the value: `field('tags').op('isEmpty')`.
- Nothing is validated on the client. The server's `Validate` is the authority.

## Query strings

```ts
import { fromQueryString, toQueryString } from 'filtering-net';

const query = toQueryString(body);            // where=<JSON>&sort=createDate:desc&sort=id:desc&page=1&pageSize=20
const restored = fromQueryString(location.search);
```

`fromQueryString` matches `where`, `sort`, `page` and `pageSize` case-insensitively, as ASP.NET does, and ignores parameters it does not own. It throws `FilterQueryStringError` (with `.parameter`) on malformed JSON, a sort item whose `:` suffix is not `asc`/`desc`, a page that `int.TryParse` would reject, or a repeated scalar parameter. On the server, bind with `[AsParameters] FilterQuery` (minimal APIs) or `[FromQuery] FilterQuery` (MVC) and call `ToRequest()`. `[AsParameters]` reads `sort` from the query only on GET, HEAD and DELETE routes; on POST, PUT, PATCH and `Map` minimal APIs infer the `SortItem` array from the body, so use `[FromQuery]` there.

Minimal APIs and `[ApiController]` controllers answer a parameter that fails to parse with a 400; other MVC controllers must check `ModelState.IsValid`. In the Development environment `RouteHandlerOptions.ThrowOnBadRequest` defaults to `true`, so behind `UseExceptionHandler` that 400 becomes a 500 unless the app sets `ThrowOnBadRequest = false` or maps `BadHttpRequestException.StatusCode`. MVC binds an empty `sort=` as an empty item rather than a binding error; `Validate` reports it.

A sort field that itself contains `:` must carry an explicit direction: `meta:key:desc` round-trips, a direction-less `meta:key` does not.

## Versioning

Released in lockstep with the Filtering.Net NuGet packages. The npm version always equals the NuGet version: `filtering-net@X.Y.Z` matches `Filtering.Net X.Y.Z`.
