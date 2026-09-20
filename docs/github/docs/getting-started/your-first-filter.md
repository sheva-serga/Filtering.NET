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

The `[Map]` methods are `static partial` placeholders the generator reads — they have no body and are never called at runtime. Use `nameof(...)` to keep them refactor-safe. `Sortable = true` opts the property into the `sort` array of an incoming request.

## What the generator emits

For the partial above, the generator emits a sibling source file containing:

- An implementation of `IFilterDefinition<User>` on `UserFilter`.
- `Validate(FilterRequest)` plus three companion `Validate` overloads (filter node, sort list, page/pageSize) that surface structured `FilterValidationError`s.
- `ApplyFilter(IQueryable<User>, FilterNode?)` — typed predicates per `(property, operator)` pair, no expression-tree construction at runtime.
- `ApplySorting(IQueryable<User>, sort, page, pageSize)` — typed `OrderBy` / `ThenBy` chains.
- A registration into the assembly-wide `services.AddFiltering()` extension method as a singleton `IFilterDefinition<User>`.

Cross-link to [How it works](../concepts/how-it-works.md) for the compile-time pipeline that drives this emission.

!!! note
    The class must be `partial` and must not declare a base class. The generated part derives from `FilterDefinition<User>`, which is where `Validate`, `ApplyFilter`, and `ApplySorting` are implemented.

## See also

- [DI registration](di-registration.md)
- [Your first request](your-first-request.md)
- [How it works](../concepts/how-it-works.md)
