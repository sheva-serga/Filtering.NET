---
title: IQueryable<T>.ApplyPagedAsync
description: Async EF Core sibling of Apply that issues CountAsync + ToListAsync and returns a PageResult<T>.
---

# `IQueryable<T>.ApplyPagedAsync(IFilterDefinition<T>, FilterRequest, CancellationToken)`

## Purpose

Async EF Core extension that pairs a [`FilterRequest`](filter-request.md) with an [`IFilterDefinition<T>`](i-filter-definition.md) and produces a materialised, paged result. Unlike the sync [`Apply`](apply-extension.md), this method actually executes against the database — it issues `CountAsync` over the filtered query, then `ToListAsync` over the sort/page slice, and bundles both into a [`PageResult<T>`](page-result.md). Lives in the `Filtering.Net.EntityFrameworkCore` package.

## Signature

```csharp
namespace Filtering.Net.EntityFrameworkCore;

public static class FilteringEntityFrameworkExtensions
{
    public static Task<PageResult<TEntity>> ApplyPagedAsync<TEntity>(
        this IQueryable<TEntity> query,
        IFilterDefinition<TEntity> definition,
        FilterRequest request,
        CancellationToken cancellationToken = default);
}
```

## Members

| Parameter | Type | Description |
|-----------|------|-------------|
| `query` | `IQueryable<TEntity>` | The base queryable (typically a DbSet or a pre-narrowed projection). Throws `ArgumentNullException` if `null`. |
| `definition` | [`IFilterDefinition<TEntity>`](i-filter-definition.md) | The generated filter definition. Throws `ArgumentNullException` if `null`. |
| `request` | [`FilterRequest`](filter-request.md) | The structured filter/sort/page request. Throws `ArgumentNullException` if `null`. |
| `cancellationToken` | `CancellationToken` | Forwarded to `CountAsync` and `ToListAsync`. |
| **Returns** | `Task<`[`PageResult<TEntity>`](page-result.md)`>` | The paged result. |
| **Throws** | `FilterValidationException` | When validation fails. |

## Behavior

1. Null-check arguments.
2. `definition.Validate(request)` — throws `FilterValidationException` if invalid.
3. `ApplyFilter` if `Where` is non-null.
4. `await CountAsync(...)` over the filtered query — captures the unfiltered-by-paging total.
5. `ApplySorting` over the filtered query if either `Sort` is non-empty or `Page`/`PageSize` is non-null.
6. `await ToListAsync(...)` over the sort/page slice.
7. Resolve `Page` (max with 1) and `PageSize` (defaults to materialised count when no paging was requested) and return `new PageResult<TEntity>(items, totalCount, page, pageSize)`.

## Examples

```csharp
app.MapPost("/users/search", async (
    FilterRequest request,
    IFilterDefinition<User> filter,
    AppDbContext db,
    CancellationToken ct) =>
{
    var page = await db.Users.AsNoTracking().ApplyPagedAsync(filter, request, ct);
    return Results.Ok(page);
});
```

## See also

- [`IQueryable<T>.Apply`](apply-extension.md)
- [`PageResult<T>`](page-result.md)
- [Async paged queries](../../guides/async-paged-queries.md)
