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
      { "field": "Name", "operator": "contains", "value": "ali" },
      { "field": "IsActive", "operator": "eq", "value": true }
    ]
  },
  "sort": [{ "field": "Age", "dir": 1 }],
  "page": 1,
  "pageSize": 25
}
```

## Nodes — Group vs Leaf

`FilterNode` is the abstract base. Two concrete shapes derive from it:

- **`FilterGroup`** — `{ "and": [...] }` or `{ "or": [...] }`. Carries a list of child nodes and a `LogicalOp` (`And = 0`, `Or = 1`). Groups can nest arbitrarily deep, bounded by `MaxNestingDepth` in the filter class's `[FilterDefaults]`.
- **`FilterLeaf`** — `{ "field": "...", "operator": "...", "value": ... }`. The value's JSON kind is checked against the operator's expected shape during validation.

A nested example combining both groups:

```json
{
  "where": {
    "or": [
      { "field": "IsActive", "operator": "eq", "value": false },
      {
        "and": [
          { "field": "Age", "operator": "gte", "value": 18 },
          { "field": "Name", "operator": "startsWith", "value": "A" }
        ]
      }
    ]
  }
}
```

This selects users who are inactive *or* who are at least 18 *and* whose name starts with `A`.

## Polymorphic deserialization

`FilterNodeJsonConverter` discriminates on the shape of the JSON object:

- Presence of `field` (with sibling `operator` and `value`) → deserialize as `FilterLeaf`.
- Presence of `and` or `or` → deserialize as `FilterGroup`.

There is no `$type` discriminator and no `JsonPolymorphic` attribute. Clients post the natural shape and the converter picks the right runtime type.

For typed leaf values in trim / AOT scenarios, the converter consults a `JsonSerializerContext` passed via the `services.AddFiltering(IJsonTypeInfoResolver)` overload. Every value type that appears on the wire — `string`, `int`, `Guid`, custom structs, etc. — must be registered with `[JsonSerializable]` on that context.

## SortItem and SortDir

`SortItem` is a flat record:

- `field` — string name of a property declared `Sortable = true` in its `[Map]`.
- `dir` — `SortDir` enum: `Asc = 0`, `Desc = 1`. Clients post the integer; the converter reads it as the enum.

The same numeric-enum convention applies to `LogicalOp` (`And = 0`, `Or = 1`) — that's the value behind a `FilterGroup.Op` field, though most clients use the JSON `and` / `or` keys directly and never see the enum on the wire.

!!! tip
    Validation rejects sort entries whose field is not `Sortable = true` with the `NotSortable` code, and rejects unknown fields anywhere in the tree with `UnknownField`. See [Validation philosophy](validation-philosophy.md) for the full code list.

## See also

- [Validation philosophy](validation-philosophy.md)
- [Profiles and operators](profiles-and-operators.md)
- [FilterRequest reference](../reference/types/filter-request.md)
