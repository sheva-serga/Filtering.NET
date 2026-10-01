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

`fromQueryString` ignores parameters it does not own. It throws `FilterQueryStringError` (with `.parameter`) on malformed JSON, a sort item whose `:` suffix is not `asc`/`desc`, a non-integer page, or a repeated scalar parameter. On the server, bind with `[AsParameters] FilterQuery` (minimal APIs) or `[FromQuery] FilterQuery` (MVC) and call `ToRequest()`. Minimal APIs and `[ApiController]` controllers reject a parameter that fails to parse with a 400; other MVC controllers must check `ModelState.IsValid`.

A sort field that itself contains `:` cannot travel through the query string.

## Versioning

Released in lockstep with the Filtering.Net NuGet packages. The npm version always equals the NuGet version: `filtering-net@X.Y.Z` matches `Filtering.Net X.Y.Z`.
