---
title: Async paged queries with ApplyPagedAsync
description: One-call validate + filter + sort + count + page that returns PageResult<T>.
---

# Async paged queries with `ApplyPagedAsync`

## What this does

`IQueryable<TEntity>.ApplyPagedAsync(IFilterDefinition<TEntity>, FilterRequest, CancellationToken)` is the EF Core async one-shot. It validates the request, applies the filter expression and sort/paging, runs `CountAsync` over the filtered query and `ToListAsync` over the paginated query, and packages the results into a `PageResult<TEntity>` carrying `Items`, `TotalCount`, `Page`, and `PageSize`. Source: `src/Filtering.Net.EntityFrameworkCore/FilteringEntityFrameworkExtensions.cs`.

## When to use

Every EF Core endpoint returning paged data. The extension lives in `Filtering.Net.EntityFrameworkCore`; reference that package whenever you want async EF integration on top of the synchronous `IQueryable<T>.Apply(...)` extension.

## Minimal code

Lifted from `samples/UserManagement.WebApi/Controllers/UsersController.cs`:

```csharp
using Filtering.Net;
using Filtering.Net.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("users")]
public sealed class UsersController(AppDbContext dbContext, IFilterDefinition<User> userFilter) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IFilterDefinition<User> _userFilter = userFilter;

    [HttpPost("search")]
    public async Task<ActionResult<PageResult<User>>> SearchAsync(
        [FromBody] FilterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageResult = await _dbContext.Users
                .Include(user => user.Department)
                .ApplyPagedAsync(_userFilter, request, cancellationToken);

            return Ok(pageResult);
        }
        catch (FilterValidationException validationException)
        {
            return BadRequest(validationException.Result);
        }
    }
}
```

## Variations

- **Always pass the request's `CancellationToken`** — the extension forwards it to both `CountAsync` and `ToListAsync`. Cancelling a long-running paged query becomes a one-line plumbing concern at the controller.
- **Pre-narrow the source** — multi-tenancy and authorization filters belong on the `IQueryable<TEntity>` *before* you call `ApplyPagedAsync`, not inside the filter class. `dbContext.Users.Where(user => user.TenantId == tenantId).ApplyPagedAsync(...)` keeps the row-level security policy out of the filter surface.
- **Validation-only preview** — for endpoints that should report whether a request would validate without hitting the database, call `_userFilter.Validate(request)` directly and return the `FilterValidationResult` (the sample's `/users/validate` endpoint demonstrates this pattern).

## Pitfalls

- `ApplyPagedAsync` runs **two** SQL queries: a `COUNT` over the filtered query, then a paginated `SELECT`. For very large result sets where the count itself is expensive, consider keyset/cursor pagination — but that's outside this library's scope today.
- The exception that escapes `ApplyPagedAsync` for invalid input is `FilterValidationException`. `OperationCanceledException` may also escape on cancellation — handle it separately (typically you let it propagate so ASP.NET Core returns a 499/499-equivalent).
- The validation step happens *before* the database is opened. A request with bad operators or unknown fields will not consume a connection.
- `Items` is materialized in memory; the paged query is fully realized into a `List<T>`. Don't call `ApplyPagedAsync` and then `.AsEnumerable()` it back into a streaming pipeline — that's the wrong shape for this method.

## See also

- [Handling validation errors](handling-validation-errors.md)
