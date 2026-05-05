---
title: IQueryable<T>.Apply
description: Synchronous validate then filter then sort then page orchestrator.
---

# `IQueryable<T>.Apply(IFilterDefinition<T>, FilterRequest)`

## Purpose

Synchronous extension that ties an [`IFilterDefinition<T>`](i-filter-definition.md) to an `IQueryable<T>`. Validates the request first; if validation fails, throws `FilterValidationException` carrying the [`FilterValidationResult`](filter-validation-result.md). On success it composes filter, sort, and paging onto the source queryable in that order and returns the resulting `IQueryable<T>` — caller chooses how to materialise it (`ToList`, `AsAsyncEnumerable`, project further, etc.).

For an async EF-aware variant that also runs `CountAsync` + `ToListAsync` and returns a [`PageResult<T>`](page-result.md), use [`ApplyPagedAsync`](apply-paged-async.md).

## Signature

```csharp
namespace Filtering.Net;

public static class FilteringQueryableExtensions
{
    public static IQueryable<TEntity> Apply<TEntity>(
        this IQueryable<TEntity> query,
        IFilterDefinition<TEntity> definition,
        FilterRequest request);
}
```

## Members

| Parameter | Type | Description |
|-----------|------|-------------|
| `query` | `IQueryable<TEntity>` | The source queryable to compose against. |
| `definition` | [`IFilterDefinition<TEntity>`](i-filter-definition.md) | The generated filter definition for `TEntity`. Resolve from DI via the emitted `AddFiltering()` extension. |
| `request` | [`FilterRequest`](filter-request.md) | The structured filter/sort/page request. |
| **Returns** | `IQueryable<TEntity>` | The filtered/sorted/paginated queryable. |
| **Throws** | `FilterValidationException` | When `definition.Validate(request)` fails. |

## Behavior

1. `definition.Validate(request)` — throws `FilterValidationException` if invalid.
2. `definition.ApplyFilter(query, request.Where)` if `Where` is non-null.
3. `definition.ApplySorting(filteredQuery, request.Sort, request.Page, request.PageSize)` if either `Sort` is non-empty or `Page`/`PageSize` is non-null. When neither sort nor paging is requested, the filtered query is returned unchanged.

## Examples

Sync materialization:

```csharp
var results = users.Apply(filter, request).ToList();
```

Project into a DTO before materializing:

```csharp
var dtos = users.Apply(filter, request)
                .Select(u => new UserDto(u.Id, u.Email))
                .ToList();
```

## See also

- [`IQueryable<T>.ApplyPagedAsync`](apply-paged-async.md)
- [`IFilterDefinition<T>`](i-filter-definition.md)
- [Handling validation errors](../../guides/handling-validation-errors.md)
