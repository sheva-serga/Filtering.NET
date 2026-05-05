---
title: "[GenerateFilter<T>]"
description: Class-level attribute marking a partial as a generated filter for entity T.
---

# `[GenerateFilter<T>]`

## Purpose

Marks a `partial class` as the filter definition for entity type `T`. The Filtering.Net source generator walks every type carrying this attribute, extracts its `[Map]` and `[PropertyMap]` declarations, and emits the matching `IFilterDefinition<T>` implementation alongside the partial.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateFilterAttribute<TEntity> : Attribute
{
    public Type EntityType => typeof(TEntity);
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `TEntity` (type parameter) | `Type` | The entity type whose properties this filter targets. Drives `IFilterDefinition<TEntity>` and the validation/predicate signatures emitted into the partial. |

## Examples

A minimal filter declaration:

```csharp
using Filtering.Net;

[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Email))]
    public partial void Email();
}
```

The generator emits the matching `partial class UserFilter : IFilterDefinition<User>` next to this declaration.

## Related diagnostics

- [FN0008 — `[Map]` method must be partial](../diagnostics/FN0008.md)

## See also

- [Mapping properties](../../guides/mapping-properties.md)
- [`[Map]`](map.md)
