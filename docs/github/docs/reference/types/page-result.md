---
title: PageResult<T>
description: Paged query result with stored items + counts and computed paging metadata.
---

# `PageResult<T>`

## Purpose

Immutable record returned by [`ApplyPagedAsync`](apply-paged-async.md). Bundles the materialised slice of `Items` together with the unfiltered total `TotalCount`, plus the resolved `Page` and `PageSize`. Three computed members (`TotalPages`, `HasPrevious`, `HasNext`) make UI pagination metadata trivial — they're derived on read, never stored.

## Signature

```csharp
namespace Filtering.Net.EntityFrameworkCore;

public sealed record PageResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages { get; }
    public bool HasPrevious { get; }
    public bool HasNext { get; }
}
```

## Properties

### Stored

| Name | Type | Description |
|------|------|-------------|
| `Items` | `IReadOnlyList<TItem>` | Materialised items for this page in the requested order. |
| `TotalCount` | `int` | Total rows that matched the filter, across all pages. |
| `Page` | `int` | Resolved 1-based page index. Always at least `1`, even if the caller passed a smaller value or `null`. |
| `PageSize` | `int` | Page size that was actually applied. When the caller did not request paging, this falls back to the materialised count. |

### Computed (do not store)

| Name | Type | Description |
|------|------|-------------|
| `TotalPages` | `int` | `Math.Ceiling(TotalCount / PageSize)`. Returns `1` when `PageSize <= 0` to keep arithmetic well-defined. |
| `HasPrevious` | `bool` | `Page > 1`. |
| `HasNext` | `bool` | `Page < TotalPages`. |

The three computed properties are deliberately not constructor parameters. Don't try to populate them when serializing/deserializing across a wire — they round-trip through the constructor for free, and serializing them back risks divergence from the stored values.

## Examples

JSON shape (typical ASP.NET Core response):

```json
{
  "items": [
    { "id": "5fa1...", "email": "alice@example.com" },
    { "id": "8c34...", "email": "bob@example.com" }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 25,
  "totalPages": 2,
  "hasPrevious": false,
  "hasNext": true
}
```

Construction in tests:

```csharp
var page = new PageResult<User>(items: [user1, user2], totalCount: 42, page: 1, pageSize: 25);
Assert.True(page.HasNext);
Assert.Equal(2, page.TotalPages);
```

## See also

- [`IQueryable<T>.ApplyPagedAsync`](apply-paged-async.md)
- [Async paged queries](../../guides/async-paged-queries.md)
