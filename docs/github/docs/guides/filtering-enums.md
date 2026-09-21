---
title: Filtering enums
description: The generator auto-emits a profile for every enum found in the property graph.
---

# Filtering enums

## What this does

When a `[Map]` targets a property typed as a C# `enum`, the source generator scans the properties of every `[GenerateFilter<TEntity>]` entity in the assembly (via `EnumTypeCollector`) and auto-emits a profile named `Filtering.Net.Generated.<EnumName>Filter`. You do not write a `[FilterProfile<TEnum>]` declaration by hand — the generator produces it for every enum it finds and wires it to the matching `[Map]`. The operator catalogue for an enum is `eq`, `ne`, `in`, `isNull`.

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
// No Profile = typeof(...) — the generator auto-emits Filtering.Net.Generated.UserStatusFilter
// and wires it to this property automatically.
[Map(nameof(User.Status), Sortable = true)]
public partial class UserFilter { }
```

A request leaf:

```json
{ "field": "Status", "op": "in", "value": ["Active", "Pending"] }
```

## Variations

- **Query by enum name (string)** — the JSON value parses by name: `"value": "Active"`.
- **Query by numeric value (int)** — the JSON value parses by ordinal: `"value": 0` (where `Active = 0`).
- **`in` with mixed shapes** — every element of an `in` array is parsed independently, so `["Active", 1, "Banned"]` is well-formed.
- **`isNull` on a nullable enum** — when the property is `UserStatus?`, the auto-emitted profile carries `isNull` semantics that match the column's nullability.

## Pitfalls

- When the column is stored as a string in the database, register an EF Core `ValueConverter<TEnum, string>` on the model side via `modelBuilder.Entity<T>().Property(x => x.Status).HasConversion<MyEnumStringConverter>()` in `OnModelCreating`. EF then translates the generated predicate against the model-side enum into the right SQL (`WHERE status = 'Active'`) — Filtering.Net stays out of that pipeline.
- An invalid enum name in the JSON value (`"value": "Bogus"`) fails validation with `InvalidValueType` and the message `String 'Bogus' is not a valid member of enum UserStatus.` — the extractor rejects it before predicate building.
- Adding a `[FilterProfile<UserStatus>]` of your own does not suppress the auto-emitted one; both then match the CLR type, so resolution becomes ambiguous (`FN0012`) and every `[Map]` for that enum must name a profile explicitly with `Profile = typeof(MyUserStatusFilter)`.
- The short `<EnumName>Filter` name is only used when the simple name is unique in the compilation. When two enums in different namespaces share a simple name, both profiles are named after the enum's fully-qualified name instead, so nothing collides. Don't hard-code the generated name; reach the operators through your filter class.

## See also

- [Mapping properties](mapping-properties.md)
- [Profiles and operators](../concepts/profiles-and-operators.md)
