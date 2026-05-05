---
title: FilterValidationCode
description: Enum of validation failure categories, one value per detectable failure mode.
---

# `FilterValidationCode`

## Purpose

Machine-readable categorisation for every [`FilterValidationError`](filter-validation-error.md). Use this in switch expressions to translate a validation failure into a domain error, an HTTP status, or a localized client-facing message.

## Signature

```csharp
namespace Filtering.Net;

public enum FilterValidationCode
{
    UnknownField,
    OperatorNotAllowed,
    InvalidValueType,
    InvalidValueFormat,
    EmptyInArray,
    InterceptorRejected,
    NotSortable,
    InvalidSortDirection,
    PageInvalid,
    PageSizeTooLarge,
    PageSizeInvalid,
    NestingTooDeep,
    TooManyConditions,
    GroupEmpty
}
```

## Values

| Value | Triggered when |
|------|------|
| `UnknownField` | The leaf or sort directive references a field name that no `[Map]` declaration configured. |
| `OperatorNotAllowed` | The operator is not in the property's resolved profile, or was excluded by `[Map(Only = ...)]` / `[Map(Except = ...)]`. |
| `InvalidValueType` | The JSON value's `JsonValueKind` does not match what the operator expects (e.g. a bool where a number is required). |
| `InvalidValueFormat` | The JSON kind matched but the format is wrong (e.g. `"abc"` for an invariant decimal), or a `[FilterValidator]` returned a non-null message. |
| `EmptyInArray` | The `in` operator was given an empty array. |
| `InterceptorRejected` | An `[InterceptValue]`-decorated interceptor threw `FilterValidationException`. |
| `NotSortable` | A `SortItem.Field` references a property whose `[Map]` did not set `Sortable = true`. |
| `InvalidSortDirection` | A `SortItem.Dir` value is not `Asc` or `Desc`. |
| `PageInvalid` | `FilterRequest.Page` is less than `1`. |
| `PageSizeTooLarge` | `FilterRequest.PageSize` exceeds the resolved `MaxPageSize` (`[PageSettings]` or `[FilterDefaults]`). |
| `PageSizeInvalid` | `FilterRequest.PageSize` is less than `1`. |
| `NestingTooDeep` | The filter tree's `FilterGroup` nesting depth exceeds `[FilterDefaults].MaxNestingDepth`. |
| `TooManyConditions` | The total `FilterLeaf` count in the request exceeds `[FilterDefaults].MaxLeafConditions`. |
| `GroupEmpty` | An `and:[]`, `or:[]`, or `not:[]` group was supplied with zero children. |

## Examples

Map codes to HTTP status codes:

```csharp
var status = error.Code switch
{
    FilterValidationCode.UnknownField        => 404,
    FilterValidationCode.NestingTooDeep      => 413,
    FilterValidationCode.TooManyConditions   => 413,
    FilterValidationCode.PageSizeTooLarge    => 413,
    _                                        => 400
};
```

## See also

- [`FilterValidationResult`](filter-validation-result.md)
- [Handling validation errors](../../guides/handling-validation-errors.md)
