---
title: "[FilterValidator]"
description: Method-level attribute declaring a custom validator for an operator's input.
---

# `[FilterValidator]`

## Purpose

Decorates a `static` method on a `[FilterProfile<T>]` class as a value validator for one named operator. The generator emits a call to this method during request validation, after the JSON value has been deserialized to the operator's typed parameter. Returning `null` means success; returning a non-null string surfaces as a [`FilterValidationError`](../types/filter-validation-error.md) with code `InvalidValueFormat`.

Use `[FilterValidator]` for shape checks that go beyond JSON kind/type (e.g. enforcing a regex, a value range, or a non-empty array) without throwing.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class FilterValidatorAttribute(string operatorName) : Attribute
{
    public string OperatorName { get; }
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `operatorName` | `string` | Required. The operator name (matching a sibling `[FilterOperator]` on the same profile) that this validator runs for. |

## Examples

Reject overlong substring queries on a `contains` operator:

```csharp
[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class TightStringFilter
{
    [FilterValidator("contains")]
    public static string? ValidateContains(string value)
        => value.Length > 100 ? "contains value must be at most 100 characters" : null;
}
```

When the validator returns a non-null string, validation fails with code `InvalidValueFormat` and the returned message becomes the user-facing message on the `FilterValidationError`.

## Related diagnostics

No FN-rule directly targets `[FilterValidator]` misuse; missing or mistyped operator names surface via [FN0006 — Unknown operator on profile](../diagnostics/FN0006.md) when the profile is consulted.

## See also

- [`[FilterOperator]`](filter-operator.md)
- [`[InterceptValue]`](intercept-value.md)
- [Handling validation errors](../../guides/handling-validation-errors.md)
