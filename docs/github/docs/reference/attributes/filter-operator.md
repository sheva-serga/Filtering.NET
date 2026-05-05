---
title: "[FilterOperator]"
description: Method-level attribute declaring an operator on a profile.
---

# `[FilterOperator]`

## Purpose

Decorates a `public static` member on a `[FilterProfile<T>]` class as the template for one operator. The generator extracts the lambda body and inlines it into the per-property `Build` method emitted on every consumer's filter class. Inlining means there is no runtime delegate dispatch — the operator becomes part of the entity-shaped expression tree EF Core (or any LINQ provider) sees.

The decorated member must be `public static` and shaped as a lambda — typically an `Expression<Func<TColumn, TValue, bool>>` property. Method-shape overrides (taking pre-computed parameters such as a captured `DateTime` cutoff) are also supported.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class FilterOperatorAttribute(string name) : Attribute
{
    public string Name { get; }
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `name` | `string` | Required. The operator name as it appears in `FilterLeaf.Operator` (`"eq"`, `"contains"`, `"withinDays"`, …). Must be unique per profile. |

## Examples

Property-shaped operator (most common):

```csharp
[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class StringFilterExtensions
{
    [FilterOperator("startsWithCi")]
    public static Expression<Func<string, string, bool>> StartsWithCi
        => (column, value) => column.ToLower().StartsWith(value.ToLower());
}
```

Method-shaped operator with a pre-computed parameter (used to keep `DateTime.UtcNow` out of the EF expression tree — see FN1001):

```csharp
[FilterOperator("withinDays")]
public static Expression<Func<DateTime, bool>> WithinDays(int days)
{
    var cutoff = DateTime.UtcNow.AddDays(-days);
    return column => column >= cutoff;
}
```

## Related diagnostics

- [FN0011 — `[FilterOperator]` member must be public static](../diagnostics/FN0011.md)
- [FN0017 — Duplicate operator declaration on profile](../diagnostics/FN0017.md)
- [FN1001 — `DateTime.UtcNow`/`Now` used directly inside `[FilterOperator]` lambda](../diagnostics/FN1001.md)
- [FN1004 — Operator is declared but unused](../diagnostics/FN1004.md)
- [FN1007 — Operator body uses untranslatable method](../diagnostics/FN1007.md)

## See also

- [Custom profiles](../../guides/custom-profiles.md)
- [`[FilterProfile<TColumn>]`](filter-profile.md)
