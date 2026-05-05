---
title: "[ConvertWith<TConverter>]"
description: Pair an EF Core ValueConverter with a property so JSON values match the model side.
---

# `[ConvertWith<TConverter>]`

## Purpose

Tells the source generator that a mapped property is persisted through an EF Core `ValueConverter<TModel, TProvider>`. The generator then uses `TModel` (not `TProvider`) as the typed-value parameter for that property, so JSON values are deserialized into the model-side shape before predicates run. Use this whenever the database column type differs from the C# property type and EF Core handles the round-trip via a value converter.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ConvertWithAttribute<TConverter> : Attribute
{
    public Type ConverterType => typeof(TConverter);
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `TConverter` (type parameter) | `Type` | The EF Core converter type. Must inherit from `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TModel, TProvider>`. The generator extracts `TModel` from the constraint. Misuse triggers [FN0007](../diagnostics/FN0007.md). |

## Examples

A strongly-typed ID stored as `Guid`:

```csharp
public readonly record struct OrderId(Guid Value);

public sealed class OrderIdConverter : ValueConverter<OrderId, Guid>
{
    public OrderIdConverter() : base(id => id.Value, value => new OrderId(value)) { }
}

[GenerateFilter<Order>]
public partial class OrderFilter
{
    [Map(nameof(Order.Id))]
    [ConvertWith<OrderIdConverter>]
    public partial void Id();
}
```

The filter accepts JSON `Guid` values and lifts them into `OrderId` before evaluating the predicate.

## Related diagnostics

- [FN0007 — Invalid value converter type](../diagnostics/FN0007.md)

## See also

- [Value conversion](../../guides/value-conversion.md)
- [`[Map]`](map.md)
