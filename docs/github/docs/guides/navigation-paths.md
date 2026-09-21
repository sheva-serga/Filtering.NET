---
title: Navigation paths and aliases
description: Filter through related entities with [Map("Path.To.Property", Alias = "...")].
---

# Navigation paths and aliases

## What this does

`[Map("Department.Name", Alias = "departmentName")]` exposes a column from a related entity under a friendly external field name. The generator parses the dotted path against the entity model and emits an EF-translatable navigation access (`user => user.Department.Name`) at filter time. The `Alias` is the public-facing field name consumers send in `FilterRequest`.

## When to use

Any time the filterable surface needs columns that live on a related table — typical examples are user/department, order/customer, post/author. Without a navigation `[Map]` consumers would have to load the related rows separately and filter in memory; with one, the predicate stays a single SQL query that joins through the navigation.

## Minimal code

```csharp
[GenerateFilter<User>]
// Exposes Department.Name as 'departmentName' in the JSON request.
[Map("Department.Name", Profile = typeof(StringFilter), Alias = "departmentName", Sortable = true)]
public partial class UserFilter { }
```

Sample request leaf:

```json
{ "field": "departmentName", "op": "eq", "value": "Engineering" }
```

EF Core translates the predicate into a SQL join through the `Department` navigation.

The sample app reaches the same columns with [`[MapNested]`](nested-filters.md) instead, because `Department` already has its own filter class. Use a dotted `[Map]` when you want one or two columns of a related entity and there is no filter class to reuse.

## Variations

- **Multi-segment paths** — `[Map("OrgUnit.Department.Name", Alias = "departmentName")]` walks two navigations. Each segment must be a real navigation property on the preceding entity type.
- **Aliasing is mandatory for dotted paths** — a JSON `field` like `"Department.Name"` collides with reserved JSON property syntax in many client tools and surfaces the internal model shape. Always provide an `Alias` for dotted paths so consumers see a flat external name.
- **Sortable navigation paths** — `Sortable = true` works the same way; the generator emits an `OrderBy` walking the navigation.

## Pitfalls

- Paths through a nullable navigation produce `FN1006`. The emitted accessor is a plain member chain (`user => user.Department.Name`) with no `?.` anywhere — null handling is entirely the query provider's. EF Core turns it into a LEFT JOIN whose comparison simply does not match when the related row is missing, which is usually what you want; in-memory LINQ over the same lambda throws a `NullReferenceException`. If you need different semantics, carry the column with a [`[PropertyMap]` rule](property-map-overrides.md) whose accessor spells out the guard.
- A dotted path may not read a member *through* a `Nullable<T>` segment — `"Created.Year"` on a `DateTime?` is `FN0024`, because `Nullable<DateTime>` does not expose `Year`. Use a `[PropertyMap]` rule for that.
- Aliases must be unique across the filter class — a duplicate fires `FN0009` (case-insensitive comparison).
- The path must resolve against the entity model. A typo (`"Departement.Name"`) fires `FN0003`.
- The leaf type at the end of the path (`Department.Name` is a `string`) is what the profile must accept. The same profile-resolution rules apply to navigation paths as to top-level properties.

## See also

- [Mapping properties](mapping-properties.md)
- [Nested filter inlining](nested-filters.md) — when the related entity has its own `[GenerateFilter<TRelated>]` partial.
