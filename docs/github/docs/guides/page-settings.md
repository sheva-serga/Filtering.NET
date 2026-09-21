---
title: Page settings (defaults, max)
description: Set default and maximum page size on a filter class with [PageSettings].
---

# Page settings (defaults, max)

## What this does

`[PageSettings(DefaultPageSize = 25, MaxPageSize = 200)]` is a class-level attribute on a `[GenerateFilter<TEntity>]` partial. `DefaultPageSize` is the page size used when the request omits `pageSize`. `MaxPageSize` is the validation ceiling — requests asking for more rows fail with a structured error before the database is touched. Both are plain `int` named arguments; omit one and the class inherits that value from the assembly-wide `[FilterDefaults]`.

## When to use

Every filter class that backs a paged endpoint. Pinning a sensible default lets clients omit `pageSize` and still get reasonable results; pinning a max protects the database from "give me a million rows" requests.

## Minimal code

```csharp
[GenerateFilter<User>]
[PageSettings(DefaultPageSize = 25, MaxPageSize = 200)]
[Map(nameof(User.Id), Sortable = true)]
[Map(nameof(User.Name), Profile = typeof(StringFilter), Sortable = true)]
public partial class UserFilter { }
```

A request with `"pageSize": 50` runs as-is. A request with no `pageSize` resolves to 25. A request with `"pageSize": 5000` fails validation before any SQL is executed.

## Variations

- **Omit `MaxPageSize`** — the class inherits whatever ceiling is configured at the assembly level (`[assembly: FilterDefaults(MaxPageSize = ...)]`), or falls back to the library default.
- **Omit `DefaultPageSize`** — the class inherits the assembly-level `[FilterDefaults(DefaultPageSize = ...)]`, falling back to the library default of 50. There is no way to make `pageSize` mandatory: a request that omits it is paged at the resolved default, and a request that omits both `page` and `pageSize` is not paged at all.
- **Per-class override** — different filter classes can carry different `[PageSettings]` values; the per-class attribute wins over assembly-wide defaults.
- **No paging at all** — for endpoints that should never paginate (e.g. an export controller), simply don't set `Page`/`PageSize` in the request and skip `ApplyPagedAsync` in favour of `ApplyFilter` + `ApplySorting` + `ToListAsync`.

## Pitfalls

- A request whose `pageSize` exceeds `MaxPageSize` fails with `FilterValidationCode.PageSizeTooLarge`.
- A request whose `pageSize` is less than 1 fails with `FilterValidationCode.PageSizeInvalid`.
- A request whose `page` is less than 1 fails with `FilterValidationCode.PageInvalid`, as does a `page` so large that the rows it would skip overflow an `int`.
- `DefaultPageSize` only applies when the request omits `pageSize`. It does not cap inputs — that is `MaxPageSize`'s job. Setting `DefaultPageSize = 25` does not prevent a client asking for 1000.
- Paging only kicks in when the request carries `page` or `pageSize`. Ask `definition.ResolvePageSize(request.PageSize)` if you need to know the size the engine will apply before you run the query — `ApplyPagedAsync` reports that same number in `PageResult<T>.PageSize`.

## See also

- [Async paged queries with `ApplyPagedAsync`](async-paged-queries.md)
