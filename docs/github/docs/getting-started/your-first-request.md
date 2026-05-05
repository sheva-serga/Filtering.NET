---
title: Your first request
description: Send a FilterRequest from a controller and get a paged result.
---

# Your first request

With a `[GenerateFilter<User>]` partial declared and `services.AddFiltering()` wired into DI, you have everything you need to accept a `FilterRequest` from an HTTP client and return a `PageResult<User>`.

## The controller endpoint

The EF Core helpers package adds `IQueryable<T>.ApplyPagedAsync(...)`, which validates the request, applies filter + sort, runs `CountAsync` + `ToListAsync` against the provider, and packages the result into a `PageResult<T>`:

```csharp
[HttpPost("search")]
public async Task<ActionResult<PageResult<User>>> Search(
    [FromBody] FilterRequest request,
    [FromServices] IFilterDefinition<User> userFilter,
    [FromServices] AppDbContext dbContext,
    CancellationToken cancellationToken)
{
    try
    {
        var page = await dbContext.Users
            .ApplyPagedAsync(userFilter, request, cancellationToken);
        return Ok(page);
    }
    catch (FilterValidationException invalid)
    {
        return BadRequest(invalid.Result);
    }
}
```

`IFilterDefinition<User>` is the generated singleton; ASP.NET Core resolves it via `[FromServices]`. The same definition can be injected into any handler — it is stateless and thread-safe.

## The request body

Clients post a `FilterRequest` as JSON. The `where` field is a polymorphic `FilterNode` — a tree of `FilterGroup` (with `and` or `or`) and `FilterLeaf` (with `field` + `operator` + `value`) nodes:

```json
{
  "where": {
    "and": [
      { "field": "Name", "operator": "contains", "value": "ali" },
      { "field": "IsActive", "operator": "eq", "value": true }
    ]
  },
  "sort": [{ "field": "Age", "dir": 1 }],
  "page": 1,
  "pageSize": 25
}
```

The `sort` array carries `{ field, dir }` items where `dir` is `0` for ascending and `1` for descending. `page` is 1-based; `pageSize` is bounded by the filter's `[PageSettings(MaxPageSize = ...)]` attribute when present.

## What happens on validation failure

`Apply` and `ApplyPagedAsync` always run validation before they touch the underlying `IQueryable<T>`. On any validation error, they throw `FilterValidationException` whose `Result` property carries a `FilterValidationResult` — a structured list of `FilterValidationError` items with codes and JSON-pointer-style paths.

The controller above catches the exception and returns the result as the HTTP 400 body. Clients then see a typed error payload like:

```json
{
  "errors": [
    {
      "code": "OperatorNotAllowed",
      "path": "/where/and/0/operator",
      "message": "Operator 'fuzzy' is not allowed on field 'Name'."
    }
  ]
}
```

See the [handling validation errors](../guides/handling-validation-errors.md) guide for ProblemDetails mapping and middleware patterns.

## See also

- [The FilterRequest JSON shape](../concepts/filter-request-shape.md)
- [Validation philosophy](../concepts/validation-philosophy.md)
- [Handling validation errors](../guides/handling-validation-errors.md)
