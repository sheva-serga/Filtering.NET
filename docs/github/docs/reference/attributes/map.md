---
title: "[Map]"
description: Method-level attribute mapping an entity property as filterable.
---

# `[Map]`

## Purpose

Decorates a `partial` method on a `[GenerateFilter<T>]` class to declare one filterable (and optionally sortable) property. Every method that has `[Map]` must be `partial`; the generator emits the implementation. The attribute is the primary surface for tweaking how a single property participates in the filter pipeline — picking a profile, restricting operators, exposing an alias, or making the property sortable.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MapAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; }
    public Type? Profile { get; init; }
    public string[]? Only { get; init; }
    public string[]? Except { get; init; }
    public string? Alias { get; init; }
    public bool Sortable { get; init; }
    public SortDir DefaultSortDirection { get; init; } = SortDir.Asc;
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `propertyName` | `string` | Required. Name (or dotted navigation path) of the entity property to map. Use `nameof(Entity.Property)` to keep this rename-safe. |
| `Profile` | `Type?` | Optional explicit profile type. When `null`, the generator resolves a built-in profile from the property's CLR type. |
| `Only` | `string[]?` | Operator allowlist (subset of the resolved profile's operators). Mutually exclusive with `Except`. |
| `Except` | `string[]?` | Operator blocklist (subset of the resolved profile's operators). Mutually exclusive with `Only`. |
| `Alias` | `string?` | Optional alias surfaced in JSON requests instead of the entity property name. |
| `Sortable` | `bool` | When `true`, the property may also appear in `FilterRequest.Sort`. Defaults to `false`. |
| `DefaultSortDirection` | [`SortDir`](../types/sort-item.md) | Direction applied when this property is sorted without an explicit direction. Defaults to `SortDir.Asc`. |

## Examples

Basic — make a property filterable with all operators its profile defines:

```csharp
[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Email))]
    public partial void Email();
}
```

Filterable + sortable, with an alias and a non-default sort direction:

```csharp
[Map(nameof(User.CreatedAt), Sortable = true, DefaultSortDirection = SortDir.Desc, Alias = "createdAt")]
public partial void CreatedAt();
```

Restrict operators with `Only` (allowlist) or `Except` (blocklist):

```csharp
[Map(nameof(User.Email), Only = new[] { "eq", "contains" })]
public partial void Email();

[Map(nameof(User.Status), Except = new[] { "in" })]
public partial void Status();
```

Pick a custom profile explicitly:

```csharp
[Map(nameof(Order.Total), Profile = typeof(MoneyFilter))]
public partial void Total();
```

## Related diagnostics

- [FN0001 — Duplicate filter mapping](../diagnostics/FN0001.md)
- [FN0003 — Property has both `[Map]` and `[PropertyMap]`](../diagnostics/FN0003.md)
- [FN0004 — Property not found on entity](../diagnostics/FN0004.md)
- [FN0008 — `[Map]` method must be partial](../diagnostics/FN0008.md)
- [FN0012 — Alias collides with existing property or alias](../diagnostics/FN0012.md)
- [FN0014 — Interceptor declared without matching `[Map]`](../diagnostics/FN0014.md)

## See also

- [Mapping properties](../../guides/mapping-properties.md)
- [Sortable properties](../../guides/sortable-properties.md)
- [Restricting operators](../../guides/restricting-operators.md)
- [`[PropertyMap]`](property-map.md)
