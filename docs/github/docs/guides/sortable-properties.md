---
title: Sortable properties
description: Mark properties sortable with Sortable = true and set default direction.
---

# Sortable properties

## What this does

`Sortable = true` on `[Map]` opts the property into the `sort` array of an incoming `FilterRequest`. The generator emits a typed `OrderBy` / `ThenBy` chain dispatch for that property. `DefaultSortDirection = SortDir.Desc` flips the default direction for `SortItem` entries that omit `Direction`.

## When to use

Any property your UI exposes as a sortable column. The `sort` field of a request is an ordered array, so a multi-key sort like "by department, then by created-at descending" requires marking each contributing column sortable.

## Minimal code

```csharp
[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Id), Sortable = true)]
    private static partial void MapId();

    // DefaultSortDirection.Desc — a SortItem with no Direction lands newest-first.
    [Map(nameof(User.CreatedAt), Sortable = true, DefaultSortDirection = SortDir.Desc)]
    private static partial void MapCreatedAt();

    [Map(nameof(User.Age), Sortable = true, DefaultSortDirection = SortDir.Desc)]
    private static partial void MapAge();
}
```

A consumer requesting `{ "sort": [{ "field": "createdAt" }, { "field": "id", "direction": "asc" }] }` gets `OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id)`.

## Variations

- Multiple sortable properties — every `[Map]` with `Sortable = true` becomes a tie-breaker option in the `sort` array.
- Explicit per-`SortItem` direction — `{ "field": "age", "direction": "asc" }` overrides the property's `DefaultSortDirection`.
- Combine with `Alias = "..."` — the `sort[].field` matches the alias, not the underlying property name.

## Pitfalls

- Only one `[Map]` per property is allowed to set `Sortable = true`. Repeating it on multiple methods triggers `FN0002`.
- Properties with sortable-looking CLR types (numbers, dates) that are not marked `Sortable = true` raise `FN1002` as a friendly nudge.

## See also

- [Mapping properties](mapping-properties.md)
