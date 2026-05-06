---
title: Restricting operators
description: Allow-list or deny-list which operators a property accepts via Only/Except.
---

# Restricting operators

## What this does

`Only` and `Except` on `[Map]` shrink the operator set the resolved profile would otherwise expose for a property. `Only` is an allow-list — only the named operators survive. `Except` is a deny-list — the named operators are removed from the profile's full set. Validation rejects any incoming leaf whose operator is not in the effective set.

## When to use

- Hide expensive operators from public APIs (e.g. drop `contains` and keep only `eq` to force exact matches and benefit from indexes).
- Constrain a column's filtering surface to the set the UI actually exposes — closes a hole where consumers could probe with operators you never tested.

## Minimal code

Lifted from the sample app's `UserFilter.cs`:

```csharp
// Operator allow-list via Only — emails accept only equality, substring, and null-check.
[Map(nameof(User.Email), Profile = typeof(StringFilter), Sortable = true,
    Only = new[] { "eq", "contains", "isNull" })]
private static partial void MapEmail();
```

A request that sends `{ "field": "email", "op": "startsWith", "value": "..." }` fails validation with an "operator not allowed" error — even though `StringFilter` declares `startsWith`, this property does not allow it.

## Variations

- Deny-list form — `Except = new[] { "contains" }` keeps everything from the profile except the named operators.
- Both at once — `Only` is applied first, then `Except` subtracts. Useful when a custom profile inherits a wide surface and you want a curated subset minus a few items.
- Cross-property defaults — declare `[FilterDefaults(Only = new[] { ... })]` at the class level to apply the allow-list to every property unless overridden.

## Pitfalls

- `Only` and `Except` combined into an empty effective set raises `FN1005`. The property still exists, but every leaf targeting it will fail validation. Either widen the lists or remove the `[Map]` entirely.

## See also

- [Mapping properties](mapping-properties.md)
