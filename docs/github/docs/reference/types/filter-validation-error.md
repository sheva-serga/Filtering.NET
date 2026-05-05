---
title: FilterValidationError
description: One structured error with code, JSON-pointer path, and message.
---

# `FilterValidationError`

## Purpose

A single structured error from filter request validation. Errors are accumulated into a [`FilterValidationResult`](filter-validation-result.md) and surfaced verbatim (no rethrow loop) — the validator collects every issue it can find in one pass so clients fix them all at once.

`Path` follows a JSON-pointer-like syntax (`where.and[0].value`, `sort[2].field`, `pageSize`) so a UI can highlight the exact offending node.

## Signature

```csharp
namespace Filtering.Net;

public sealed record FilterValidationError(
    string Path,
    FilterValidationCode Code,
    string Message,
    string? Field = null,
    string? OperatorName = null);
```

## Properties

| Name | Type | Description |
|------|------|-------------|
| `Path` | `string` | JSON-pointer-like location of the offending node within the request (e.g. `where.and[0].value`). |
| `Code` | [`FilterValidationCode`](filter-validation-code.md) | Machine-readable category for the error. Switch on this for programmatic handling. |
| `Message` | `string` | Human-readable explanation suitable for surfacing to API clients. |
| `Field` | `string?` | Configured field name involved in the error, if applicable. |
| `OperatorName` | `string?` | Operator name involved in the error, if applicable. |

## Examples

Reading errors out of a validation result:

```csharp
var validation = filter.Validate(request);
foreach (var error in validation.Errors)
{
    logger.LogWarning(
        "Filter validation error: {Code} at {Path} (field={Field}, op={Operator}): {Message}",
        error.Code, error.Path, error.Field, error.OperatorName, error.Message);
}
```

## See also

- [`FilterValidationResult`](filter-validation-result.md)
- [`FilterValidationCode`](filter-validation-code.md)
- [Handling validation errors](../../guides/handling-validation-errors.md)
