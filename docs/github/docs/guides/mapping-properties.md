---
title: Mapping properties
description: Use [Map] to expose a property as filterable.
---

# Mapping properties

## What this does

Placing `[Map(nameof(Entity.Property))]` on a `[GenerateFilter<TEntity>]` partial class exposes that property as filterable. Add one `[Map]` per property. The source generator emits the dispatch and predicate code (per-leaf validation, JSON value extraction, typed `Where` predicate) for each `[Map]`-decorated method at compile time.

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
[Map(nameof(User.Id),       Sortable = true)]
[Map(nameof(User.Name),     Sortable = true)]
[Map(nameof(User.Age),      Sortable = true)]
[Map(nameof(User.IsActive))]
public partial class UserFilter { }
```

The class body can stay empty. It only needs members when you add an `[InterceptValue]` method or a `[PropertyMap]` rule.

## Variations

- `Alias = "displayName"` — surface the property under a different name in the JSON request.
- `Sortable = true` — opt the column into the `sort` array. See [sortable properties](sortable-properties.md).
- `DefaultSortDirection = SortDir.Desc` — flip default direction when consumers sort without specifying one.
- `Profile = typeof(MyProfile)` — pick a non-default profile (e.g. a custom `[FilterProfile<string>]` that adds operators).
- `Only = new[] { "eq", "in" }` — allow-list the operators accepted on this property.
- `Except = new[] { "contains" }` — deny-list operators inherited from the resolved profile.

Navigation paths use dotted strings: `[Map("Department.Name", Alias = "departmentName")]`.

## Pitfalls

- The filter class must be `partial` and must not declare a base class (`FN0021`): the generated part derives from `FilterDefinition<TEntity>`.
- A property may be carried by either `[Map]` or `[PropertyMap]`, never both — `FN0002` flags the conflict.
- Two `[Map]` attributes that point at the same property name (or the same alias) collide with `FN0001`.
- The string passed to `[Map(...)]` must resolve to a real property on the entity, otherwise `FN0003` fires.
- Aliases must be unique across the whole filter class (case-insensitive), or `FN0009` fires.

## See also

- [Sortable properties](sortable-properties.md)
- [Restricting operators](restricting-operators.md)
- [Built-in profiles per primitive type](built-in-profiles.md)
