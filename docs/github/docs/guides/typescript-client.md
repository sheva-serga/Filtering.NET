---
title: TypeScript client
description: Compose FilterRequest JSON and query strings from TypeScript with the filtering-net npm package.
---

# TypeScript client

## What this does

`filtering-net` is a dependency-free ESM package of small functions that return the exact JSON `FilterRequest` reads. Field names are plain strings; the server's `Validate` stays the only authority.

```sh
npm install filtering-net
```

## Minimal code

```ts
import { and, desc, field, request } from 'filtering-net';

const body = request({
  where: and(field('status').in(['Active', 'Pending']), field('createdAt').gt(new Date('2026-01-01T00:00:00Z'))),
  sort: [desc('createdAt')],
  page: 1,
  pageSize: 25,
});
// { where: { and: [ { field: 'status', op: 'in', value: [...] }, ... ] }, sort: [ { field: 'createdAt', dir: 'desc' } ], page: 1, pageSize: 25 }
```

## Building blocks

| Function | Produces |
|----------|----------|
| `field(name).eq/ne/gt/gte/lt/lte/contains/startsWith/endsWith(value)` | `{ field, op, value }` |
| `field(name).in(values)` | `{ field, op: 'in', value: [...] }`; an empty array is sent as is and matches nothing |
| `field(name).isNull()` | `{ field, op: 'isNull' }` with no `value` key |
| `field(name).op(name, value?)` | Custom operators from your profiles; omit `value` for unary ones |
| `and(...)`, `or(...)`, `not(child)` | Groups; `false`/`null`/`undefined`/`''` children are skipped; an empty group returns `undefined` |
| `asc(f)`, `desc(f)`, `sortBy(f, dir?)` | Sort items; `sortBy` without `dir` uses the property's `DefaultSortDirection` |
| `withTiebreakers(sort, ...tiebreakers)` | Appends each tiebreaker whose field is not already sorted (case-insensitive) |
| `request({ where, sort, page, pageSize })` | A `FilterRequest` without `undefined` keys or an empty `sort` |
| `toQueryString(request)` / `fromQueryString(query)` | See [Query-string binding](query-string-binding.md) |

## Pitfalls

- `Date` values become `toISOString()` (UTC). An invalid `Date` throws `RangeError`; `NaN` and `Infinity` throw `TypeError` instead of turning into `null`.
- Groups are not flattened or unwrapped: the JSON keeps exactly the structure you wrote.
- Nothing is validated on the client. Unknown fields or operators come back as a 400 from `Validate`.

## See also

- [The FilterRequest JSON shape](../concepts/filter-request-shape.md)
- [Query-string binding](query-string-binding.md)
