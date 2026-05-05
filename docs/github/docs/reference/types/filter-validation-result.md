---
title: FilterValidationResult
description: Container for FilterValidationError instances returned from Validate.
---

# `FilterValidationResult`

## Purpose

Aggregate result of validating a [`FilterRequest`](filter-request.md). Returned from every `IFilterDefinition<T>.Validate(...)` overload. Inspect [`IsValid`](#properties) for the happy-path check, or iterate [`Errors`](#properties) for structured details. The synchronous [`Apply`](apply-extension.md) and async [`ApplyPagedAsync`](apply-paged-async.md) extensions throw `FilterValidationException` carrying this result when validation fails.

## Signature

```csharp
namespace Filtering.Net;

public sealed record FilterValidationResult(IReadOnlyList<FilterValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
    public static FilterValidationResult Success { get; } = new([]);
}
```

## Properties

| Name | Type | Description |
|------|------|-------------|
| `Errors` | `IReadOnlyList<`[`FilterValidationError`](filter-validation-error.md)`>` | All validation errors collected for the request. Empty on success. |
| `IsValid` | `bool` | `true` when `Errors.Count == 0`. |
| `Success` (static) | `FilterValidationResult` | Shared zero-error instance returned on the success path to avoid allocations. |

## Examples

Validate and surface errors as an HTTP 400 payload:

```csharp
var validation = filter.Validate(request);
if (!validation.IsValid)
    return Results.BadRequest(validation);
```

Serialised JSON of a result with two errors:

```json
{
  "errors": [
    {
      "path": "where.and[0].value",
      "code": "InvalidValueType",
      "message": "Expected a string, got Number.",
      "field": "email",
      "operatorName": "contains"
    },
    {
      "path": "sort[0].field",
      "code": "NotSortable",
      "message": "Field 'email' is not configured as sortable.",
      "field": "email",
      "operatorName": null
    }
  ]
}
```

## See also

- [`FilterValidationError`](filter-validation-error.md)
- [`FilterValidationCode`](filter-validation-code.md)
- [Validation philosophy](../../concepts/validation-philosophy.md)
