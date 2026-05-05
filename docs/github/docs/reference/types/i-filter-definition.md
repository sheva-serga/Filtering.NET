---
title: IFilterDefinition<T>
description: Composite interface every emitted filter class implements. Carries every Validate / ApplyFilter / ApplySorting overload.
---

# `IFilterDefinition<T>`

## Purpose

Composite contract every source-generated filter class implements. The `[GenerateFilter<T>]` partial gets a generated counterpart that fulfils this interface; consumers never write an implementation by hand. Resolve the interface from DI (the generator-emitted `AddFiltering()` extension wires every filter class into `IServiceCollection`) and pass the instance to [`Apply`](apply-extension.md) / [`ApplyPagedAsync`](apply-paged-async.md), or call its members directly when you need finer control over the validate-then-apply sequence.

## Signature

```csharp
namespace Filtering.Net;

public interface IFilterDefinition<TEntity>
{
    FilterValidationResult Validate(FilterNode? where);
    FilterValidationResult Validate(IReadOnlyList<SortItem>? sortItems);
    FilterValidationResult Validate(int? page, int? pageSize);
    FilterValidationResult Validate(FilterRequest request);

    IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, FilterNode? where);
    IQueryable<TEntity> ApplySorting(
        IQueryable<TEntity> query,
        IReadOnlyList<SortItem>? sortItems,
        int? page = null,
        int? pageSize = null);
}
```

## Members

### Validation

| Member | Description |
|--------|-------------|
| `Validate(FilterNode?)` | Validates a single filter expression tree. Returns [`FilterValidationResult.Success`](filter-validation-result.md) when `where` is `null`. |
| `Validate(IReadOnlyList<SortItem>?)` | Validates a list of sort directives — checks each `Field` is mapped as sortable and each `Dir` is a defined `SortDir` value. |
| `Validate(int?, int?)` | Validates `page`/`pageSize` against the resolved `[PageSettings]`/`[FilterDefaults]` bounds. |
| `Validate(FilterRequest)` | Aggregate over the three above; returns a single result with errors from all sub-validations. |

### Application

| Member | Description |
|--------|-------------|
| `ApplyFilter(IQueryable<TEntity>, FilterNode?)` | Composes the predicate tree onto `query` and returns the filtered `IQueryable<TEntity>`. Pass-through when `where` is `null`. Caller is responsible for having validated the input first. |
| `ApplySorting(IQueryable<TEntity>, IReadOnlyList<SortItem>?, int?, int?)` | Applies sorting and (optionally) paging. When no sort items are supplied but paging is, an internal stable order is applied so `Skip`/`Take` is deterministic. |

## Examples

Resolve from DI and call directly:

```csharp
public sealed class UserSearchHandler(IFilterDefinition<User> filter, AppDbContext db)
{
    public async Task<List<User>> Search(FilterRequest request, CancellationToken ct)
    {
        var validation = filter.Validate(request);
        if (!validation.IsValid)
            throw new FilterValidationException(validation);

        var query = db.Users.AsNoTracking();
        query = filter.ApplyFilter(query, request.Where);
        query = filter.ApplySorting(query, request.Sort, request.Page, request.PageSize);
        return await query.ToListAsync(ct);
    }
}
```

Most consumers use the [`Apply`](apply-extension.md) / [`ApplyPagedAsync`](apply-paged-async.md) extensions instead, which orchestrate this sequence.

## See also

- [`IQueryable<T>.Apply`](apply-extension.md)
- [`IQueryable<T>.ApplyPagedAsync`](apply-paged-async.md)
- [How it works](../../concepts/how-it-works.md)
