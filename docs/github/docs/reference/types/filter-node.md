---
title: FilterNode (FilterGroup, FilterLeaf)
description: Abstract base of the filter tree, with FilterGroup (logical And/Or/Not) and FilterLeaf (field/operator/value) subclasses.
---

# `FilterNode` / `FilterGroup` / `FilterLeaf`

## Purpose

`FilterNode` is the abstract root of the filter expression tree carried by [`FilterRequest.Where`](filter-request.md). It has exactly two concrete subtypes:

- [`FilterGroup`](#filtergroup) — combines child nodes with a `LogicalOp` (And, Or, Not).
- [`FilterLeaf`](#filterleaf) — a single `field` / `op` / `value` triple.

Polymorphic JSON deserialization is handled automatically by the bundled `FilterNodeJsonConverter`.

## Signature

```csharp
namespace Filtering.Net;

[JsonConverter(typeof(FilterNodeJsonConverter))]
public abstract record FilterNode;

public sealed record FilterGroup(LogicalOp Op, IReadOnlyList<FilterNode> Children) : FilterNode;

public sealed record FilterLeaf(string Field, string Operator, JsonElement Value) : FilterNode;
```

## Properties

### `FilterGroup`

| Name | Type | Description |
|------|------|-------------|
| `Op` | [`LogicalOp`](sort-item.md) | Combinator: `And`, `Or`, or `Not`. |
| `Children` | `IReadOnlyList<FilterNode>` | Child nodes. For `LogicalOp.Not`, must contain exactly one child; the JSON converter enforces this. |

### `FilterLeaf`

| Name | Type | Description |
|------|------|-------------|
| `Field` | `string` | Configured property name (or `Alias`) being filtered. |
| `Operator` | `string` | Operator name (`"eq"`, `"contains"`, …). |
| `Value` | `System.Text.Json.JsonElement` | Raw JSON value, kept untyped here and parsed per-leaf by the resolved profile during validation. |

## JSON shape

The converter discriminates on the first property key it sees:

| Key present | Resolves to |
|------|------|
| `field` | `FilterLeaf` |
| `and` | `FilterGroup(LogicalOp.And, …)` |
| `or` | `FilterGroup(LogicalOp.Or, …)` |
| `not` | `FilterGroup(LogicalOp.Not, [single child])` |

Mixing `field` with any of `and`/`or`/`not` is a JSON error; mixing two group keys is also a JSON error.

## Examples

A single leaf:

```json
{ "field": "email", "op": "contains", "value": "@example.com" }
```

A group of two leaves joined by `And`:

```json
{
  "and": [
    { "field": "status", "op": "eq", "value": "Active" },
    { "field": "age",    "op": "gte", "value": 18 }
  ]
}
```

A `not`-group (exactly one child):

```json
{ "not": [ { "field": "isArchived", "op": "eq", "value": true } ] }
```

## See also

- [Filter request shape](../../concepts/filter-request-shape.md)
- [`FilterRequest`](filter-request.md)
- [`SortItem` / `SortDir` / `LogicalOp`](sort-item.md)
