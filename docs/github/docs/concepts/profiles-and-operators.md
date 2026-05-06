---
title: Profiles and operators
description: How [FilterProfile<T>] groups operators by column type, and how custom profiles extend them.
---

# Profiles and operators

## What a profile is

A *profile* is a static class decorated with `[FilterProfile<TColumn>]` that names a set of operators applicable to a column of CLR type `TColumn`. Operators are exposed as `[FilterOperator("name")]`-decorated methods that return `Expression<Func<TColumn, TValue, bool>>` (binary) or `Expression<Func<TColumn, bool>>` (unary).

Profiles decouple "which operators exist" from "which property uses them". A property picks a profile via `[Map(..., Profile = typeof(MyProfile))]`, or — when omitted — the generator's `ProfileResolver` picks a built-in by CLR type.

## Built-in profiles

Filtering.Net ships profiles for every common scalar in `src/Filtering.Net/Profiles/`:

- **`StringFilter`** — `eq`, `contains`, `startsWith`, `endsWith`, `in`, `isNull`, `notNull`.
- **`Numeric/Int32Filter`, `Int64Filter`, `DecimalFilter`, `DoubleFilter`, `SingleFilter`, `Int16Filter`, `ByteFilter`, `SByteFilter`, `UInt16Filter`, `UInt32Filter`, `UInt64Filter`** — `eq`, `lt`, `lte`, `gt`, `gte`, `in`, `between`, `isNull`, `notNull`.
- **`BoolFilter`** — `eq`, `isNull`, `notNull`.
- **`GuidFilter`** — `eq`, `in`, `isNull`, `notNull`.
- **`DateTimeFilter`** and the matching `Temporal/*` profiles for `DateOnly`, `TimeOnly`, `DateTimeOffset`, `TimeSpan` — `eq`, `lt`, `lte`, `gt`, `gte`, `between`, `isNull`, `notNull`.
- **Auto-emitted `<EnumName>Filter`** — for every enum referenced by a `[GenerateFilter<T>]` graph, the generator emits an `[FilterProfile<TEnum>]` static class with `eq`, `in`, `isNull`, `notNull`.

The emitted SQL matches what you would write by hand: `LIKE` for `contains` / `startsWith` / `endsWith`, `IN` for `in`, `BETWEEN` for `between`, `IS NULL` / `IS NOT NULL` for the unary forms.

## Profile resolution

When `[Map]` does not specify `Profile = typeof(...)`, the generator runs `ProfileResolver` against the property's declared CLR type:

- Exact match → pick the built-in (`string` → `StringFilter`, `int` → `Int32Filter`, etc.).
- Enum type → pick the auto-emitted `<EnumName>Filter`.
- Multiple matches → emit `FN0014` and require the consumer to pick one explicitly.
- No match → emit `FN0008` and require `Profile = typeof(...)`.

Setting `Profile = typeof(MyProfile)` overrides the resolver entirely and bypasses both ambiguity and unmatched-type diagnostics. The chosen profile's `TColumn` must be assignment-compatible with the property's CLR type, or `FN0005` fires.

## Custom profiles inherit via BasedOn

Adding an operator to an existing surface is a six-line custom profile. Use `[BasedOn(typeof(...))]` to inherit every operator from the base profile, then add your own with `[FilterOperator]`:

```csharp
[FilterProfile<string>]
public static class StringFilterPlus
{
    [BasedOn(typeof(StringFilter))] private static void Inherit() { }

    [FilterOperator("fuzzy")]
    public static Expression<Func<string, string, bool>> Fuzzy() =>
        (column, value) => column.ToLower().Contains(value.ToLower());
}
```

Then `[Map(nameof(User.Name), Profile = typeof(StringFilterPlus))]` makes `fuzzy` available on `User.Name`. The lambda body is inlined into the per-property `Build` method — no delegate dispatch at runtime.

The analyzer also runs the body through its EF translatability allow-list and warns with `FN1007` if you call a method EF Core cannot translate.

## See also

- [Custom profiles guide](../guides/custom-profiles.md)
- [How it works](how-it-works.md)
