---
title: Mapping properties
description: Use [Map] to expose a property as filterable.
---

# Mapping properties

## What this does

Placing `[Map(nameof(Entity.Property))]` on a `private static partial void` method inside a `[GenerateFilter<TEntity>]` partial exposes that property as filterable. The source generator emits the dispatch and predicate code (per-leaf validation, JSON value extraction, typed `Where` predicate) for each `[Map]`-decorated method at compile time.

## When to use

Any column you want consumers to filter on. The four-method partial below is the minimum viable filter class — declare one `[Map]` per filterable column on the entity.

## Minimal code

```csharp
public sealed class User
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";
    public int    Age      { get; set; }
    public bool   IsActive { get; set; }
}

[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Id),       Sortable = true)] private static partial void MapId();
    [Map(nameof(User.Name),     Sortable = true)] private static partial void MapName();
    [Map(nameof(User.Age),      Sortable = true)] private static partial void MapAge();
    [Map(nameof(User.IsActive))]                  private static partial void MapIsActive();
}
```

The method body is empty — the generator never invokes it; it only reads the attribute. The method is purely a declaration site for the metadata.

## Variations

- `Alias = "displayName"` — surface the property under a different name in the JSON request.
- `Sortable = true` — opt the column into the `sort` array. See [sortable properties](sortable-properties.md).
- `DefaultSortDirection = SortDir.Desc` — flip default direction when consumers sort without specifying one.
- `Profile = typeof(MyProfile)` — pick a non-default profile (e.g. a custom `[FilterProfile<string>]` that adds operators).
- `Only = new[] { "eq", "in" }` — allow-list the operators accepted on this property.
- `Except = new[] { "contains" }` — deny-list operators inherited from the resolved profile.

Navigation paths use dotted strings: `[Map("Department.Name", Alias = "departmentName")]`.

## Pitfalls

- The `[Map]`-decorated method must be declared `partial`, otherwise `FN0008` fires.
- A property may be carried by either `[Map]` or `[PropertyMap]`, never both — `FN0003` flags the conflict.
- Two `[Map]` methods that point at the same property name (or the same alias) collide with `FN0001`.
- The string passed to `[Map(...)]` must resolve to a real property on the entity, otherwise `FN0004` fires.
- Aliases must be unique across the whole filter class (case-insensitive), or `FN0012` fires.

## See also

- [Sortable properties](sortable-properties.md)
- [Restricting operators](restricting-operators.md)
- [Built-in profiles per primitive type](built-in-profiles.md)
- [FN0008 — `[Map]` method must be partial](../reference/diagnostics/FN0008.md)
