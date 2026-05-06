---
title: Built-in profiles per primitive type
description: How the generator picks a profile by CLR type when Profile is omitted.
---

# Built-in profiles per primitive type

## What this does

When `[Map]` does not specify `Profile = typeof(...)`, the generator's `ProfileResolver` picks a built-in profile by the property's declared CLR type. Each built-in profile ships a fixed operator set tailored to that type — string substring matching, numeric comparisons, temporal range checks, etc.

## When to use

Always, unless you have a reason to opt out. Letting the resolver pick is the lowest-friction path: you declare `[Map(nameof(User.Age))]` on an `int` property and get `eq`, `gt`, `lt`, `in`, `between`, and friends for free. Reach for `Profile = typeof(MyProfile)` only when you need operators the built-in does not provide, or when multiple profiles match the same CLR type and you must disambiguate.

## CLR type mapping

| CLR type | Profile | Operators |
|----------|---------|-----------|
| `string` | `StringFilter` | `eq`, `ne`, `contains`, `startsWith`, `endsWith`, `in`, `isNull` |
| `int`, `long`, `short`, `byte`, `sbyte`, `ushort`, `uint`, `ulong` | `Numeric/Int32Filter`, `Int64Filter`, etc. | `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `in`, `isNull` |
| `decimal`, `double`, `float` | `DecimalFilter`, `DoubleFilter`, `SingleFilter` | `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `in`, `isNull` |
| `bool` | `BoolFilter` | `eq`, `isNull` |
| `Guid` | `GuidFilter` | `eq`, `ne`, `in`, `isNull` |
| `DateTime` | `DateTimeFilter` | `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `isNull` |
| `DateTimeOffset` | `Temporal/DateTimeOffsetFilter` | `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `isNull` |
| `DateOnly` | `Temporal/DateOnlyFilter` | `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `isNull` |
| `TimeOnly` | `Temporal/TimeOnlyFilter` | `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `isNull` |
| any `enum` | auto-emitted `<EnumName>Filter` | `eq`, `ne`, `in`, `isNull` |

Nullable reference and value-typed columns share the underlying type's profile — `string?` resolves to `StringFilter`, `int?` resolves to `Int32Filter`. The `isNull` operator uses the nullable form internally.

## Variations

- Override per-property — `[Map(nameof(User.Name), Profile = typeof(StringFilterPlus))]` opts that property into a custom profile. Useful when most string columns want the built-in surface but a few need extras.
- Auto-emitted enum profiles — every enum referenced in the entity graph picks up an auto-generated `<EnumName>Filter` profile. You don't need to declare anything; the resolver finds it.

## Pitfalls

- Declaring any custom `[FilterProfile<T>]` for a CLR type that already has a built-in makes resolution ambiguous and fires `FN0014`. From that point forward, every `[Map]` for properties of that type must specify `Profile = typeof(...)` explicitly. The sample app's `StringFilterPlus` is exactly this case — adding it forces every `[Map]` on a `string` column to name a profile.
- Mapping a property whose CLR type has no built-in match (a custom struct, a third-party value type) without `Profile = typeof(...)` raises `FN0008`. Either declare a custom profile for the type or specify one on the map.

## See also

- [Defining custom profiles](custom-profiles.md)
- [Profiles and operators](../concepts/profiles-and-operators.md)
