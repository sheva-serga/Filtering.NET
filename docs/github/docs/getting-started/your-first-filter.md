---
title: Your first filter
description: Declare a [GenerateFilter<T>] partial class.
---

# Your first filter

A *filter class* is a partial class decorated with `[GenerateFilter<TEntity>]`. The source generator reads the `[Map]` attributes on the class and emits the schema that the `FilterDefinition<TEntity>` engine runs on, so the class implements `IFilterDefinition<TEntity>`.

## Declare your entity

Start with a plain POCO. Filtering.Net does not require any base class, interface, or attribute on the entity:

```csharp
public sealed class User
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = "";
    public int    Age       { get; set; }
    public bool   IsActive  { get; set; }
}
```

## Declare a filter partial

In the same project (it doesn't need to be the same file), declare a `partial class` with `[GenerateFilter<User>]` and one `[Map]` attribute per filterable property:

```csharp
[GenerateFilter<User>]
[Map(nameof(User.Id),       Sortable = true)]
[Map(nameof(User.Name),     Sortable = true)]
[Map(nameof(User.Age),      Sortable = true)]
[Map(nameof(User.IsActive))]
public partial class UserFilter { }
```

One `[Map]` attribute per filterable property sits on the class itself; use `nameof(...)` to keep it refactor-safe. `Sortable = true` opts the property into the `sort` array of an incoming request.

## What the generator emits

For the partial above, the generator emits a sibling source file containing:

- The other half of the partial, declaring `FilterDefinition<User>` as its base class.
- A constructor (plus a second one taking an `IJsonTypeInfoResolver` when the class has an operator that deserializes a typed value).
- `CreateSchema(...)` — one `FilterProperty.Map(...)` entry per `[Map]`, one `AddNested(...)` call per `[MapNested]`, and the class's page settings.
- A registration into the assembly-wide `services.AddFiltering()` extension method as a singleton `IFilterDefinition<User>`.

`Validate`, `ApplyFilter`, and `ApplySorting` are not emitted: they are ordinary library code on `FilterDefinition<User>`, which composes predicates per request from the schema.

Cross-link to [How it works](../concepts/how-it-works.md) for the compile-time pipeline that drives this emission.

!!! note
    The class must be `partial`, must sit directly in a namespace (not nested in another type, `FN0027`), must be non-generic, and must not declare a base class (`FN0020`). The generated part derives from `FilterDefinition<User>`, which is where `Validate`, `ApplyFilter`, and `ApplySorting` are implemented.

## See also

- [DI registration](di-registration.md)
- [Your first request](your-first-request.md)
- [How it works](../concepts/how-it-works.md)
