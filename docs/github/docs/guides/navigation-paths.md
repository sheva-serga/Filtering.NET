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

Lifted from `samples/UserManagement.WebApi/Filters/UserFilter.cs`:

```csharp
[GenerateFilter<User>]
public partial class UserFilter
{
    // Exposes Department.Name as 'departmentName' in the JSON request.
    [Map("Department.Name", Profile = typeof(StringFilter), Alias = "departmentName", Sortable = true)]
    private static partial void MapDepartmentName();
}
```

Sample request leaf:

```json
{ "field": "departmentName", "op": "eq", "value": "Engineering" }
```

EF Core translates the predicate into a SQL join through the `Department` navigation.

## Variations

- **Multi-segment paths** — `[Map("OrgUnit.Department.Name", Alias = "departmentName")]` walks two navigations. Each segment must be a real navigation property on the preceding entity type.
- **Aliasing is mandatory for dotted paths** — a JSON `field` like `"Department.Name"` collides with reserved JSON property syntax in many client tools and surfaces the internal model shape. Always provide an `Alias` for dotted paths so consumers see a flat external name.
- **Sortable navigation paths** — `Sortable = true` works the same way; the generator emits an `OrderBy` walking the navigation.

## Pitfalls

- Paths through a nullable navigation produce `FN1006` (potential null-propagation surprise). The generated predicate uses C# `?.` semantics, but providers translate that with their own null-handling — review the generated SQL or supply a custom mapping with explicit null guards if the default behaviour is wrong for your domain.
- Aliases must be unique across the filter class — a duplicate fires `FN0011` (case-insensitive comparison).
- The path must resolve against the entity model. A typo (`"Departement.Name"`) fires `FN0004`.
- The leaf type at the end of the path (`Department.Name` is a `string`) is what the profile must accept. The same profile-resolution rules apply to navigation paths as to top-level properties.

## See also

- [Mapping properties](mapping-properties.md)
