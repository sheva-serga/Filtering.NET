---
title: FilterRequest
description: Top-level structured filter / sort / page request bound to incoming HTTP payloads.
---

# `FilterRequest`

## Purpose

Top-level immutable record describing one filter/sort/page request. Bind it to your HTTP body (or query string) and pass it to [`Apply`](apply-extension.md) or [`ApplyPagedAsync`](apply-paged-async.md) along with a generated `IFilterDefinition<T>`. All four properties are nullable, so a fully-omitted request runs as "everything, default paging."

## Signature

```csharp
namespace Filtering.Net;

public sealed record FilterRequest
{
    public FilterNode? Where { get; init; }
    public IReadOnlyList<SortItem>? Sort { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
```

## Properties

| Name | Type | Description |
|------|------|-------------|
| `Where` | [`FilterNode?`](filter-node.md) | Filter expression tree. `null` means no `WHERE` clause. |
| `Sort` | `IReadOnlyList<`[`SortItem`](sort-item.md)`>?` | Ordered list of sort directives. `null` or empty means no explicit sort. |
| `Page` | `int?` | 1-based page index. `null` means no paging on the page index axis. |
| `PageSize` | `int?` | Page size. `null` inherits the [`[FilterDefaults]`](../attributes/filter-defaults.md)/[`[PageSettings]`](../attributes/page-settings.md) default. |

## Examples

JSON shape:

```json
{
  "where": {
    "and": [
      { "field": "status", "op": "eq", "value": "Active" },
      { "field": "createdAt", "op": "gte", "value": "2026-01-01T00:00:00Z" }
    ]
  },
  "sort": [
    { "field": "createdAt", "dir": "Desc" }
  ],
  "page": 1,
  "pageSize": 25
}
```

Bound in an ASP.NET Core endpoint:

```csharp
app.MapPost("/users/search", async (
    FilterRequest request,
    IFilterDefinition<User> filter,
    AppDbContext db,
    CancellationToken ct) =>
{
    var page = await db.Users.ApplyPagedAsync(filter, request, ct);
    return Results.Ok(page);
});
```

## JSON polymorphism

`Where` is a [`FilterNode`](filter-node.md) — an abstract record with two concrete shapes (`FilterGroup`, `FilterLeaf`). The internal `FilterNodeJsonConverter` discriminates on property keys: `field` ⇒ leaf, `and`/`or`/`not` ⇒ group. The converter is registered via `[JsonConverter]` on `FilterNode` itself, so the standard `System.Text.Json` defaults pick it up without extra setup.

## See also

- [`FilterNode`](filter-node.md)
- [`SortItem` / `SortDir` / `LogicalOp`](sort-item.md)
- [Filter request shape](../../concepts/filter-request-shape.md)
