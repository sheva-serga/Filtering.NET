---
title: "[InterceptValue]"
description: Method-level attribute that runs a transform on the typed value before predicate building.
---

# `[InterceptValue]`

## Purpose

Decorates a method on a `[GenerateFilter<T>]` partial as a value interceptor for one mapped property. The interceptor runs after JSON deserialization and before validation/predicate building, giving the consumer a chance to normalise inputs (case-fold, trim, parse compound values) or reject them by throwing [`FilterValidationException`](../types/filter-validation-result.md) — which surfaces as a `FilterValidationError` with code `InterceptorRejected`.

The interceptor method must be visible to the emitted partial — declare it `internal` or `public`. The corresponding property must already have a `[Map]`; an interceptor without a matching `[Map]` triggers [FN0014](../diagnostics/FN0014.md).

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class InterceptValueAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; }
    public bool Raw { get; init; }
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `propertyName` | `string` | Required. Name of the mapped property whose values are intercepted. |
| `Raw` | `bool` | When `false` (default) the interceptor receives the deserialized typed value. When `true` it receives the raw `System.Text.Json.JsonElement` and must return the typed value — useful for accepting alternative shapes (e.g. coercing a string into an enum the standard converter would reject). |

## Examples

Typed interceptor — trim and lowercase email lookups:

```csharp
[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Email))]
    public partial void Email();

    [InterceptValue(nameof(User.Email))]
    internal static string NormalizeEmail(string value, InterceptContext context)
        => value.Trim().ToLowerInvariant();
}
```

Raw interceptor — accept either a `string` or a `string[]` for the same operator:

```csharp
[InterceptValue(nameof(User.Tags), Raw = true)]
internal static string[] ParseTags(JsonElement element, InterceptContext context)
    => element.ValueKind switch
    {
        JsonValueKind.Array  => element.EnumerateArray().Select(e => e.GetString()!).ToArray(),
        JsonValueKind.String => new[] { element.GetString()! },
        _ => throw new FilterValidationException(...)
    };
```

## Related diagnostics

- [FN0010 — Duplicate value interceptor](../diagnostics/FN0010.md)
- [FN0014 — Interceptor declared without matching `[Map]`](../diagnostics/FN0014.md)

## See also

- [Intercepting values](../../guides/intercepting-values.md)
- [`[Map]`](map.md)
