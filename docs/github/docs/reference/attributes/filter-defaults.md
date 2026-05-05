---
title: "[FilterDefaults]"
description: Assembly-level defaults for paging and request-size limits inherited by every generated filter class.
---

# `[FilterDefaults]`

## Purpose

Sets assembly-wide defaults that the generator bakes into every emitted filter class. The values control paging fall-back behavior plus two safety limits on the filter expression tree. Per-class `[PageSettings]` overrides the page-size values; the nesting/leaf limits cannot be overridden per class.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class FilterDefaultsAttribute : Attribute
{
    public int DefaultPageSize { get; init; } = 50;
    public int MaxPageSize { get; init; } = 200;
    public int MaxNestingDepth { get; init; } = 10;
    public int MaxLeafConditions { get; init; } = 50;
}
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `DefaultPageSize` | `int` | `50` | Page size applied when `FilterRequest.PageSize` is `null`. |
| `MaxPageSize` | `int` | `200` | Inclusive upper bound on `FilterRequest.PageSize`. Requests exceeding this fail validation with code `PageSizeTooLarge`. |
| `MaxNestingDepth` | `int` | `10` | Maximum depth of nested `FilterGroup`s allowed. Exceeding the limit yields `NestingTooDeep`. |
| `MaxLeafConditions` | `int` | `50` | Maximum total `FilterLeaf` count in one request. Exceeding the limit yields `TooManyConditions`. |

## Examples

Tighten the defaults assembly-wide:

```csharp
using Filtering.Net;

[assembly: FilterDefaults(
    DefaultPageSize = 25,
    MaxPageSize = 100,
    MaxNestingDepth = 6,
    MaxLeafConditions = 30)]
```

Place the line in any `.cs` file in the consumer assembly (a dedicated `AssemblyInfo.cs` is conventional).

## Related diagnostics

No FN-rule directly targets `[FilterDefaults]`. Misuse surfaces at runtime through [`FilterValidationCode`](../types/filter-validation-code.md) values `PageSizeTooLarge`, `NestingTooDeep`, and `TooManyConditions`.

## See also

- [Restricting operators](../../guides/restricting-operators.md)
- [Page settings](../../guides/page-settings.md)
- [`[PageSettings]`](page-settings.md)
- [`[Map]`](map.md)
