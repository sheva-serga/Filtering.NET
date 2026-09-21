---
title: Profiles and operators
description: How [FilterProfile<T>] groups operators by column type, and how custom profiles extend them.
---

# Profiles and operators

## What a profile is

A *profile* is a static class decorated with `[FilterProfile<TColumn>]` that names a set of operators applicable to a column of CLR type `TColumn`. Operators are exposed as `public static` `[FilterOperator("name")]`-decorated members — properties or methods — whose type is `Expression<Func<TColumn, TValue, bool>>` (binary) or `Expression<Func<TColumn, bool>>` (unary). A member of any other type is `FN0028`.

Every profile also has a runtime form, `FilterProfile<TColumn>`. Built-in profiles expose it as `StringFilter.Profile` and friends; for your own profiles the generator emits the instance. You only deal with it directly when [building a definition by hand](../guides/hand-built-definitions.md).

Profiles decouple "which operators exist" from "which property uses them". A property picks a profile via `[Map(..., Profile = typeof(MyProfile))]`, or — when omitted — the generator's `ProfileResolver` picks a built-in by CLR type.

## Built-in profiles

Filtering.Net ships profiles for every common scalar in `src/Filtering.Net/Profiles/`:

- **`StringFilter`** — `eq`, `ne`, `contains`, `startsWith`, `endsWith`, `in`, `isNull`.
- **`Numeric/Int32Filter`, `Int64Filter`, `Int16Filter`, `ByteFilter`, `DecimalFilter`, `DoubleFilter`, `SingleFilter`** — `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `in`, `isNull`.
- **`BoolFilter`** — `eq`, `isNull`.
- **`GuidFilter`** — `eq`, `ne`, `in`, `isNull`.
- **`DateTimeFilter`** and the `Temporal/*` profiles for `DateTimeOffset`, `DateOnly`, and `TimeOnly` — `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `isNull`.
- **Auto-emitted `<EnumName>Filter`** — for every enum referenced by a `[GenerateFilter<T>]` graph, the generator emits a `[FilterProfile<TEnum>]` static class with `eq`, `ne`, `in`, `isNull`.

There is no `between` and no `notNull`; a two-value operator is a [`[PropertyMap]` override](../guides/property-map-overrides.md), and "not null" is a `not` group wrapped around an `isNull` leaf. `DateOnlyFilter` and `TimeOnlyFilter` ship in the package's `net8.0` asset only — `DateOnly` and `TimeOnly` do not exist under `netstandard2.0`.

The emitted SQL matches what you would write by hand: `LIKE` for `contains` / `startsWith` / `endsWith`, `IN` for `in`, `IS NULL` for `isNull`.

## Profile resolution

When `[Map]` does not specify `Profile = typeof(...)`, the generator runs `ProfileResolver` against the property's declared CLR type:

- Exact match → pick the built-in (`string` → `StringFilter`, `int` → `Int32Filter`, etc.).
- Enum type → pick the auto-emitted `<EnumName>Filter`.
- Multiple matches → emit `FN0012` and require the consumer to pick one explicitly.
- No match → emit `FN0006` and require `Profile = typeof(...)`.

Setting `Profile = typeof(MyProfile)` overrides the resolver entirely and bypasses both ambiguity and unmatched-type diagnostics. The named type must itself be marked `[FilterProfile<TColumn>]`, or `FN0022` fires; when it is a built-in profile, its `TColumn` must match the property's CLR type, or `FN0004` fires.

## Custom profiles inherit via BasedOn

Adding an operator to an existing surface is a six-line custom profile. Set `BasedOn = typeof(...)` to inherit every operator from the base profile, then add your own with `[FilterOperator]`:

```csharp
[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class StringFilterPlus
{
    [FilterOperator("fuzzy")]
    public static Expression<Func<string, string, bool>> Fuzzy =>
        (column, value) => column.ToLower().Contains(value.ToLower());
}
```

Then `[Map(nameof(User.Name), Profile = typeof(StringFilterPlus))]` makes `fuzzy` available on `User.Name`. At runtime the operator is part of a `FilterProfile<string>` instance that the generator emits for `StringFilterPlus`. The engine substitutes the property's accessor for `column` and the parsed value for `value`.

The analyzer also runs the body through its EF translatability allow-list and warns with `FN1007` if you call a method EF Core cannot translate.

## See also

- [Custom profiles guide](../guides/custom-profiles.md)
- [How it works](how-it-works.md)
