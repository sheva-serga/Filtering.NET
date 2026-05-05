---
title: "[PropertyMap]"
description: Per-property override that replaces the generated predicate logic with a fluent For/Operator DSL.
---

# `[PropertyMap]`

## Purpose

Marks a method as a per-property override. Instead of letting the generator pick a profile and inline its operators, the method body declares operators by hand using a fluent `For<TEntity, TColumn>(...).Operator("name", (col, val) => ...)` chain. Use `[PropertyMap]` when a property needs predicate logic that no profile expresses — multi-column composites, JSON path lookups, custom comparison semantics, etc.

A given property can have either a `[Map]` or a `[PropertyMap]`, never both.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PropertyMapAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; }
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `propertyName` | `string` | Required. Logical name of the property whose mapping this method overrides. Surfaces in filter requests and validator paths. |

## DSL shape

The body of a `[PropertyMap]` method calls `For<TEntity, TColumn>(x => x.Path)` to anchor the column expression, then chains one `.Operator("name", (col, val) => ...)` per operator. Each operator's lambda returns a predicate over the anchored column and the typed value. See [Property map overrides](../../guides/property-map-overrides.md) for the full walkthrough.

## Examples

```csharp
[GenerateFilter<Product>]
public partial class ProductFilter
{
    [PropertyMap(nameof(Product.Sku))]
    public partial void Sku() => For<Product, string>(p => p.Sku)
        .Operator("eq",       (col, value) => col == value)
        .Operator("startsWith", (col, value) => col.StartsWith(value));
}
```

## Related diagnostics

- [FN0003 — Property has both `[Map]` and `[PropertyMap]`](../diagnostics/FN0003.md)
- [FN0006 — Unknown operator on profile](../diagnostics/FN0006.md)

## See also

- [Property map overrides](../../guides/property-map-overrides.md)
- [`[Map]`](map.md)
