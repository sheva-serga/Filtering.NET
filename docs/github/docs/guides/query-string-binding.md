---
title: Query-string binding
description: Send a FilterRequest in a URL and bind it in ASP.NET Core with FilterQuery.
---

# Query-string binding

## What this does

A request can travel in a URL instead of a body:

```
GET /api/users?where=<URL-encoded JSON tree>&sort=createdAt:desc&sort=id&page=1&pageSize=20
```

- `where` is the same JSON tree the body uses.
- `sort` repeats once per item: `field`, `field:asc` or `field:desc`. The suffix is case-insensitive and splits on the last `:`.
- `page` and `pageSize` are integers.

`FilterNode` and `SortItem` expose `TryParse(string?, IFormatProvider?, out T)`, which ASP.NET Core minimal APIs and MVC (.NET 7+) discover on their own. `FilterQuery` collects the four parameters.

## Minimal code

```csharp
app.MapGet("/api/users", async ([AsParameters] FilterQuery query, AppDbContext db, IFilterDefinition<User> filter, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await db.Users.AsQueryable().ApplyPagedAsync(filter, query.ToRequest(), cancellationToken));
    }
    catch (FilterValidationException exception)
    {
        return Results.BadRequest(exception.Result);
    }
});
```

MVC, on an `[ApiController]`: `public IActionResult Get([FromQuery] FilterQuery query) => ...query.ToRequest()...`. On a controller without `[ApiController]`, check `ModelState.IsValid` first: a parameter that fails `TryParse` only adds a model-state error there, and the action would run with `query.Where == null` and return the unfiltered set.

From TypeScript, `toQueryString(request)` writes exactly this format and `fromQueryString(location.search)` reads it back (see [TypeScript client](typescript-client.md)).

## Pitfalls

- A parameter that does not parse (`where=5`, `sort=name:up`, `page=abc`) is a binding failure. Minimal APIs and `[ApiController]` controllers turn it into a 400 on their own; other MVC controllers must check `ModelState.IsValid`. Validation codes still come only from `Validate`, the same as for a body.
- A field name that itself contains `:` cannot be sorted through the query string.
- URLs have length limits (proxies commonly cap them around 8 KB). Large trees belong in a `POST` body.
- `FilterQuery` exists because `FilterRequest.Sort` is an `IReadOnlyList<SortItem>`, which query binding cannot fill. Use `ToRequest()` rather than binding `FilterRequest` directly.

## See also

- [The FilterRequest JSON shape](../concepts/filter-request-shape.md)
- [Handling validation errors](handling-validation-errors.md)
