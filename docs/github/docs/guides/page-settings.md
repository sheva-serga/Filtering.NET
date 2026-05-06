---
title: Page settings (defaults, max)
description: Set default and maximum page size on a filter class with [PageSettings].
---

# Page settings (defaults, max)

## What this does

`[PageSettings(DefaultPageSize = 25, MaxPageSize = 200)]` is a class-level attribute on a `[GenerateFilter<TEntity>]` partial. `DefaultPageSize` is the page size used when the request omits `pageSize`. `MaxPageSize` is the validation ceiling — requests asking for more rows fail with a structured error before the database is touched. Both values are `int?` and inherit from the assembly-wide `[FilterDefaults]` when omitted.

## When to use

Every filter class that backs a paged endpoint. Pinning a sensible default lets clients omit `pageSize` and still get reasonable results; pinning a max protects the database from "give me a million rows" requests.

## Minimal code

```csharp
[GenerateFilter<User>]
[PageSettings(DefaultPageSize = 25, MaxPageSize = 200)]
public partial class UserFilter
{
    [Map(nameof(User.Id), Sortable = true)]
    private static partial void MapId();

    [Map(nameof(User.Name), Profile = typeof(StringFilter), Sortable = true)]
    private static partial void MapName();

    // ... more [Map] methods
}
```

A request with `"pageSize": 50` runs as-is. A request with no `pageSize` resolves to 25. A request with `"pageSize": 5000` fails validation before any SQL is executed.

## Variations

- **Omit `MaxPageSize`** — the class inherits whatever ceiling is configured at the assembly level (`[assembly: FilterDefaults(MaxPageSize = ...)]`), or falls back to the library default.
- **Omit `DefaultPageSize`** — callers must specify `pageSize` explicitly. A request without one fails with `PageSizeInvalid` (rather than silently defaulting).
- **Per-class override** — different filter classes can carry different `[PageSettings]` values; the per-class attribute wins over assembly-wide defaults.
- **No paging at all** — for endpoints that should never paginate (e.g. an export controller), simply don't set `Page`/`PageSize` in the request and skip `ApplyPagedAsync` in favour of `ApplyFilter` + `ApplySorting` + `ToListAsync`.

## Pitfalls

- A request whose `pageSize` exceeds `MaxPageSize` fails with `FilterValidationCode.PageSizeTooLarge`.
- A request whose `pageSize` is less than 1 fails with `FilterValidationCode.PageSizeInvalid`.
- A request whose `page` is less than 1 fails with `FilterValidationCode.PageInvalid`.
- `DefaultPageSize` only applies when the request omits `pageSize`. It does not cap inputs — that is `MaxPageSize`'s job. Setting `DefaultPageSize = 25` does not prevent a client asking for 1000.

## See also

- [Async paged queries with `ApplyPagedAsync`](async-paged-queries.md)
