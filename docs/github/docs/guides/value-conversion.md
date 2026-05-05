---
title: Value conversion with [ConvertWith<TConverter>]
description: Pair an EF Core ValueConverter with a property so JSON values match the model side.
---

# Value conversion with `[ConvertWith<TConverter>]`

## What this does

`[ConvertWith<TConverter>]` declares that a property uses an EF Core `ValueConverter<TModel, TProvider>`. The deserializer reads the JSON value as `TModel` (the application-side type) and the generator builds the predicate against the model side too. EF Core then applies the converter when translating the predicate to SQL — keeping JSON inputs uniform with the C# domain model regardless of how the value is stored in the database.

## When to use

- **Enums stored as strings** — `UserStatus.Active` ⇄ `"Active"` in the column.
- **Booleans stored as `0`/`1`** — keep `bool` in C# while the column is an `int`.
- **JSON-stored complex objects** — domain object ⇄ serialized JSON string.
- **UTC ⇄ local time conversions** — `DateTime` in C# ⇄ stored as Unix epoch / local time / a normalized form.

The win is symmetric: consumers send the natural model-side value in JSON and the generated predicate uses the same model side, so EF's converter transparently produces the right SQL.

## Minimal code

```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Filtering.Net;

public enum UserStatus { Active, Suspended, Archived }

public sealed class User
{
    public int        Id     { get; set; }
    public UserStatus Status { get; set; }
}

// Stores UserStatus as its string name in the database.
public sealed class UserStatusConverter : ValueConverter<UserStatus, string>
{
    public UserStatusConverter()
        : base(status => status.ToString(),
               text   => (UserStatus)Enum.Parse(typeof(UserStatus), text)) { }
}

[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Id), Sortable = true)]
    private static partial void MapId();

    // The deserializer reads JSON values as UserStatus (the model side); EF Core's
    // converter handles the string<->enum projection at SQL translation time.
    [Map(nameof(User.Status), Sortable = true)]
    [ConvertWith<UserStatusConverter>]
    private static partial void MapStatus();
}
```

A request sending `{ "field": "status", "op": "eq", "value": "Active" }` deserializes to `UserStatus.Active`, then the generated predicate composes against the model-side enum, and EF emits `WHERE status = 'Active'` (or whatever the converter produces).

## Variations

- **Type-specific converters** — one converter per domain type, hand-written for the conversion you need.
- **Reusable enum-as-string converters** — a generic `EnumStringConverter<TEnum>` you reuse on every enum property by writing `[ConvertWith<EnumStringConverter<UserStatus>>]`.
- **JSON-stored objects** — converter side serializes the object to a JSON string; the property's filterable surface is the model object, with operators provided by a custom profile.

## Pitfalls

- The type passed to `[ConvertWith<...>]` must inherit `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TModel, TProvider>`. Anything else fires `FN0007`.

## See also

- [Filtering enums](filtering-enums.md)
- [ConvertWith attribute reference](../reference/attributes/convert-with.md)
- [FN0007 — invalid value converter type](../reference/diagnostics/FN0007.md)
