---
title: Sortable properties
description: Mark properties sortable with Sortable = true and set default direction.
---

# Sortable properties

## What this does

`Sortable = true` on `[Map]` opts the property into the `sort` array of an incoming `FilterRequest`. The engine applies a typed `OrderBy` / `ThenBy` chain for the requested fields. `DefaultSortDirection = SortDir.Desc` flips the default direction for `SortItem` entries that omit `dir`.

## When to use

Any property your UI exposes as a sortable column. The `sort` field of a request is an ordered array, so a multi-key sort like "by department, then by created-at descending" requires marking each contributing column sortable.

## Minimal code

```csharp
[GenerateFilter<User>]
[Map(nameof(User.Id), Sortable = true)]
// DefaultSortDirection.Desc — a SortItem with no dir lands newest-first.
[Map(nameof(User.CreatedAt), Sortable = true, DefaultSortDirection = SortDir.Desc)]
[Map(nameof(User.Age), Sortable = true, DefaultSortDirection = SortDir.Desc)]
public partial class UserFilter { }
```

A consumer requesting `{ "sort": [{ "field": "createdAt" }, { "field": "id", "dir": 0 }] }` gets `OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id)`.

## Variations

- Multiple sortable properties — every `[Map]` with `Sortable = true` becomes a tie-breaker option in the `sort` array.
- Explicit per-`SortItem` direction — `{ "field": "age", "dir": 0 }` overrides the property's `DefaultSortDirection`.
- Combine with `Alias = "..."` — the `sort[].field` matches the alias, not the underlying property name.

## Pitfalls

- Only one `[Map]` per property is allowed (regardless of `Sortable` setting). Duplicates trigger `FN0001 DuplicateMapping`.
- Properties with sortable-looking CLR types (numbers, dates) that are not marked `Sortable = true` raise `FN1002` as a friendly nudge.
- `dir` is a numeric enum on the wire (`0` = ascending, `1` = descending). No string-enum converter is registered, so `"dir": "asc"` does not bind — the member stays null and the property's `DefaultSortDirection` is used instead. A number outside the enum fails validation with `InvalidSortDirection` at `sort[<index>].dir`.
- A sort item with a missing or empty `field` fails validation with `NotSortable`.

## See also

- [Mapping properties](mapping-properties.md)
