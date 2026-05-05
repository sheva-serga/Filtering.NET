---
title: SortItem / SortDir / LogicalOp
description: Sort directive record, sort direction enum, and group logical-operator enum.
---

# `SortItem` / `SortDir` / `LogicalOp`

## Purpose

The three small types that round out a [`FilterRequest`](filter-request.md):

- `SortItem` — a single sort directive (field + direction).
- `SortDir` — sort direction (Ascending or Descending).
- `LogicalOp` — combinator on a [`FilterGroup`](filter-node.md) (And, Or, Not).

## Signatures

```csharp
namespace Filtering.Net;

public sealed record SortItem(string Field, SortDir Dir = SortDir.Asc);

public enum SortDir
{
    Asc  = 0,
    Desc = 1
}

public enum LogicalOp
{
    And = 0,
    Or  = 1,
    Not = 2
}
```

## Properties

### `SortItem`

| Name | Type | Description |
|------|------|-------------|
| `Field` | `string` | The configured sortable field name (or alias). Must correspond to a `[Map(..., Sortable = true)]`-decorated property. |
| `Dir` | `SortDir` | Sort direction. Defaults to `SortDir.Asc`. |

### `SortDir`

| Value | Description |
|------|------|
| `Asc` (0) | Ascending order. |
| `Desc` (1) | Descending order. |

### `LogicalOp`

| Value | Description |
|------|------|
| `And` (0) | All children must match. |
| `Or` (1) | At least one child must match. |
| `Not` (2) | The single child must not match. |

## Examples

JSON `sort` array — order by `createdAt` desc, then `email` asc:

```json
"sort": [
  { "field": "createdAt", "dir": "Desc" },
  { "field": "email",     "dir": "Asc" }
]
```

`LogicalOp.Not` group (exactly one child):

```json
{ "not": [ { "field": "isArchived", "op": "eq", "value": true } ] }
```

## See also

- [`FilterRequest`](filter-request.md)
- [`FilterNode`](filter-node.md)
