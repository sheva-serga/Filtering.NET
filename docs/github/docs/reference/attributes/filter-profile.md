---
title: "[FilterProfile<TColumn>]"
description: Class-level attribute declaring a profile of operators for a column type.
---

# `[FilterProfile<TColumn>]`

## Purpose

Marks a `static` class as the filter profile for CLR type `TColumn`. The generator builds a profile index keyed by `TColumn` and resolves each `[Map]`-ed property's profile by looking up its CLR type. A profile groups one or more `[FilterOperator]`-decorated members, optionally inheriting operators from a base profile via `BasedOn`.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FilterProfileAttribute<T> : Attribute
{
    public Type? BasedOn { get; init; }
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `T` (type parameter) | `Type` | The CLR column type this profile targets. Properties whose CLR type matches `T` (and that don't override `Profile = typeof(...)` on their `[Map]`) resolve to this profile. |
| `BasedOn` | `Type?` | Optional base profile whose operators are inherited. When `null`, the profile defines all of its operators from scratch — meaning it must also declare the required value-extractor methods. See [FN0016](../diagnostics/FN0016.md). |

## Examples

Extend a built-in profile with one extra operator:

```csharp
using Filtering.Net;
using Filtering.Net.Profiles;

[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class CaseInsensitiveStringFilter
{
    [FilterOperator("equalsIgnoreCase")]
    public static Expression<Func<string, string, bool>> EqualsIgnoreCase
        => (column, value) => column.ToLower() == value.ToLower();
}
```

Standalone profile (no `BasedOn`):

```csharp
[FilterProfile<Money>]
public static class MoneyFilter
{
    [FilterOperator("eq")]
    public static Expression<Func<Money, Money, bool>> Eq => (column, value) => column == value;

    [FilterOperator("gt")]
    public static Expression<Func<Money, Money, bool>> Gt => (column, value) => column > value;

    // Required extractor methods (TryGetScalar / TryGetArray) live here too — see FN0016.
}
```

## Related diagnostics

- [FN0009 — No inferable profile for property type](../diagnostics/FN0009.md)
- [FN0013 — `[FilterProfile.BasedOn]` references a non-profile type](../diagnostics/FN0013.md)
- [FN0015 — Multiple filter profiles match property type](../diagnostics/FN0015.md)
- [FN0016 — Standalone filter profile is missing a required extractor method](../diagnostics/FN0016.md)
- [FN1003 — Filter profile is declared but unused](../diagnostics/FN1003.md)

## See also

- [Custom profiles](../../guides/custom-profiles.md)
- [`[FilterOperator]`](filter-operator.md)
