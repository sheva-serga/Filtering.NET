---
title: Filtering enums
description: The generator auto-emits a profile for every enum found in the property graph.
---

# Filtering enums

## What this does

When a `[Map]` targets a property typed as a C# `enum`, the source generator scans the entity's property graph (via `EnumTypeCollector`) and auto-emits a profile named `Filtering.Net.Generated.<EnumName>Filter`. You do not write a `[FilterProfile<TEnum>]` declaration by hand — the generator produces it for every reachable enum and wires it to the matching `[Map]`. The default operator catalogue for an enum is `eq`, `ne`, `in`, `isNull`.

## When to use

Every `[Map]` whose target property is a C# enum. This is the zero-boilerplate path: declare the enum, declare the property, declare the `[Map]`, and filtering just works.

## Minimal code

Lifted from `samples/UserManagement.WebApi/Models/User.cs` and `Filters/UserFilter.cs`:

```csharp
public enum UserStatus
{
    Active,
    Pending,
    Suspended,
    Banned,
}

public sealed class User
{
    public int Id { get; set; }
    public UserStatus Status { get; set; }
    // ... other properties
}

[GenerateFilter<User>]
public partial class UserFilter
{
    // No Profile = typeof(...) — the generator auto-emits Filtering.Net.Generated.UserStatusFilter
    // and wires it to this property automatically.
    [Map(nameof(User.Status), Sortable = true)]
    private static partial void MapStatus();
}
```

A request leaf:

```json
{ "field": "Status", "operator": "in", "value": ["Active", "Pending"] }
```

## Variations

- **Query by enum name (string)** — the JSON value parses by name: `"value": "Active"`.
- **Query by numeric value (int)** — the JSON value parses by ordinal: `"value": 0` (where `Active = 0`).
- **`in` with mixed shapes** — every element of an `in` array is parsed independently, so `["Active", 1, "Banned"]` is well-formed.
- **`isNull` on a nullable enum** — when the property is `UserStatus?`, the auto-emitted profile carries `isNull` semantics that match the column's nullability.

## Pitfalls

- When the column is stored as a string in the database (the typical EF Core value-converter setup), pair the `[Map]` with `[ConvertWith<EnumStringConverter>]` so EF emits `WHERE status = 'Active'` instead of `WHERE status = 0`. Without the converter the generated predicate composes against the model-side enum (numeric by default) and the column type and value type misalign.
- An invalid enum name in the JSON value (`"value": "Bogus"`) fails validation with `InvalidValueFormat` — the deserializer rejects it before predicate building.
- Adding a `[FilterProfile<UserStatus>]` of your own collides with the auto-emitted one; remove the auto-emission path by selecting your custom profile via `[Map(..., Profile = typeof(MyUserStatusFilter))]` on every relevant `[Map]`.

## See also

- [Value conversion with `[ConvertWith<TConverter>]`](value-conversion.md)
- [Mapping properties](mapping-properties.md)
- [Profiles and operators](../concepts/profiles-and-operators.md)
