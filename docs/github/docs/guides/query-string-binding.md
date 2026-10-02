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

`FilterNode` and `SortItem` expose `TryParse(string?, IFormatProvider?, out T)`, which ASP.NET Core minimal APIs and MVC (.NET 7+) discover on their own. `FilterQuery` collects the four parameters; their names are matched case-insensitively, by ASP.NET and by the TypeScript client alike.

!!! note
    Since 0.3.0 `FilterNode` and `SortItem` count as simple (parseable) types for ASP.NET Core. An endpoint parameter that is a bare `FilterNode` or `SortItem` is inferred from the query string instead of the JSON body; add `[FromBody]` to keep binding it from the body. `FilterRequest` is unaffected.

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

`[AsParameters] FilterQuery` reads `sort` from the query only on `GET` and `DELETE` routes. On `POST`, `PUT`, `PATCH` or `app.Map`, minimal APIs infer the `SortItem[]` from the JSON body, so `?sort=` is silently ignored while `where`, `page` and `pageSize` still bind from the query. Use MVC `[FromQuery] FilterQuery` there, or bind `[FromQuery] SortItem[]? sort` as its own parameter.

MVC, on an `[ApiController]`: `public IActionResult Get([FromQuery] FilterQuery query) => ...query.ToRequest()...`. On a controller without `[ApiController]`, check `ModelState.IsValid` first: a parameter that fails `TryParse` only adds a model-state error there, and the action would run with `query.Where == null` and return the unfiltered set.

From TypeScript, `toQueryString(request)` writes exactly this format and `fromQueryString(location.search)` reads it back (see [TypeScript client](typescript-client.md)).

## Pitfalls

- A parameter that does not parse (`where=5`, `sort=name:up`, `page=abc`) is a binding failure. Minimal APIs and `[ApiController]` controllers turn it into a 400 on their own; other MVC controllers must check `ModelState.IsValid`. Validation codes still come only from `Validate`, the same as for a body.
- In the Development environment `RouteHandlerOptions.ThrowOnBadRequest` defaults to `true`, so a minimal-API binding failure is thrown as `BadHttpRequestException` and reaches `UseExceptionHandler` as a 500 instead of a 400. Set `services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false)` (or `PostConfigure`), or map `BadHttpRequestException.StatusCode` in the exception handler.
- A repeated scalar parameter (`page=1&page=2`, two `where`) is a 400 on minimal APIs; MVC binds the first value.
- An empty `sort=` is a 400 on minimal APIs; MVC binds it as a null item, which `Validate` reports as `NotSortable`.
- A sort field that itself contains `:` must carry an explicit direction: `meta:key:desc` round-trips, a direction-less `meta:key` does not.
- URLs have length limits. Kestrel rejects a request line over 8 KB (`MaxRequestLineSize`) with 414; IIS and in-process hosting reject a query string over 2048 bytes by default (`maxQueryString`) with 404.15. Percent-encoding roughly doubles the JSON's size, so large trees and long `in` lists belong in a `POST` body.
- `FilterQuery` exists because `FilterRequest.Sort` is an `IReadOnlyList<SortItem>`, which query binding cannot fill. Use `ToRequest()` rather than binding `FilterRequest` directly.

## See also

- [The FilterRequest JSON shape](../concepts/filter-request-shape.md)
- [Handling validation errors](handling-validation-errors.md)
