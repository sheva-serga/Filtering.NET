---
title: The FilterRequest JSON shape
description: Structured filter / sort / page request as a typed JSON tree.
---

# The FilterRequest JSON shape

A `FilterRequest` is a typed JSON document that carries everything a client needs to express a filter, a sort order, and a page selection. Polymorphism is handled by the bundled `FilterNodeJsonConverter` — clients never need to declare `$type` discriminators.

## Top-level structure

`FilterRequest` has four fields:

- `where` — a `FilterNode` tree (group or leaf), or `null` to mean "match everything".
- `sort` — an array of `SortItem` (`field` + `dir`), or `null` for the entity's default order.
- `page` — 1-based page index.
- `pageSize` — items per page, bounded by `[PageSettings(MaxPageSize = ...)]` when present on the filter class.

```json
{
  "where": {
    "and": [
      { "field": "Name", "op": "contains", "value": "ali" },
      { "field": "IsActive", "op": "eq", "value": true }
    ]
  },
  "sort": [{ "field": "Age", "dir": 1 }],
  "page": 1,
  "pageSize": 25
}
```

## Nodes — Group vs Leaf

`FilterNode` is the abstract base. Two concrete shapes derive from it:

- **`FilterGroup`** — `{ "and": [...] }`, `{ "or": [...] }`, or `{ "not": [...] }`. Carries a list of child nodes and a `LogicalOp` (`And = 0`, `Or = 1`, `Not = 2`). A `not` group takes exactly one child; any other count is a `JsonException` on deserialization. When a hand-built group is validated, zero children is `GroupEmpty` and more than one is `InvalidNodeShape`. Groups can nest arbitrarily deep, bounded by `MaxNestingDepth` from the assembly-level `[FilterDefaults]` (default 10). A request may hold at most `MaxLeafConditions` leaves (default 50).
- **`FilterLeaf`** — `{ "field": "...", "op": "...", "value": ... }`. The value is kept as a raw `JsonElement` and checked against the operator's expected shape during validation. `value` may be omitted entirely, which is how a unary operator such as `isNull` is sent.

A nested example combining both groups:

```json
{
  "where": {
    "or": [
      { "field": "IsActive", "op": "eq", "value": false },
      {
        "and": [
          { "field": "Age", "op": "gte", "value": 18 },
          { "field": "Name", "op": "startsWith", "value": "A" }
        ]
      }
    ]
  }
}
```

This selects users who are inactive *or* who are at least 18 *and* whose name starts with `A`.

Negation wraps a single child:

```json
{
  "where": {
    "not": [
      { "field": "Status", "op": "eq", "value": "Banned" }
    ]
  }
}
```

Combining `not` with `isNull` is how you express "is not null", since no profile ships a `notNull` operator.

## Polymorphic deserialization

`FilterNodeJsonConverter` discriminates on the shape of the JSON object:

- Presence of `field` (with sibling `op` and optional `value`) → deserialize as `FilterLeaf`.
- Presence of `and`, `or`, or `not` → deserialize as `FilterGroup`. More than one of the three, or one of them alongside `field`, is a `JsonException`.

There is no `$type` discriminator and no `JsonPolymorphic` attribute. Clients post the natural shape and the converter picks the right runtime type.

The converter itself reads a leaf's `value` as a raw `JsonElement` and uses no reflection, so it needs no `JsonSerializerContext`. The resolver passed to `services.AddFiltering(IJsonTypeInfoResolver)` is used later, when an operator that takes a *typed* value deserializes that element — see [Trim / AOT-clean setup](../guides/aot-clean-setup.md).

## SortItem and SortDir

`SortItem` is a flat record:

- `field` — string name of a property declared `Sortable = true` in its `[Map]`.
- `dir` — `SortDir` enum: `Asc = 0`, `Desc = 1`. It is nullable; clients post the integer, and no string-enum converter is registered, so `"asc"` fails deserialization with a `JsonException`. When `dir` is omitted the property's `DefaultSortDirection` applies; a value outside the enum fails validation with `InvalidSortDirection`.

The same numeric-enum convention applies to `LogicalOp` (`And = 0`, `Or = 1`, `Not = 2`) — that's the value behind a `FilterGroup.Op` field, though most clients use the JSON `and` / `or` / `not` keys directly and never see the enum on the wire.

!!! tip
    Validation rejects sort entries whose field is not `Sortable = true` with the `NotSortable` code, and rejects unknown fields anywhere in the tree with `UnknownField`. See [Validation philosophy](validation-philosophy.md) for the full code list.

## See also

- [Validation philosophy](validation-philosophy.md)
- [Profiles and operators](profiles-and-operators.md)
