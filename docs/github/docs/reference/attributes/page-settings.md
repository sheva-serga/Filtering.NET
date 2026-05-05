---
title: "[PageSettings]"
description: Class-level attribute setting default and maximum page size for a single filter class.
---

# `[PageSettings]`

## Purpose

Overrides the assembly-wide `[FilterDefaults]` page-size values for one `[GenerateFilter<T>]` class. Use this when a single endpoint has different paging needs from the rest of the assembly — e.g. a heavy report that allows larger pages, or a small tooltip query that should never paginate above 10 rows.

Either property may be omitted; an omitted value inherits the assembly-level default.

## Signature

```csharp
namespace Filtering.Net;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PageSettingsAttribute : Attribute
{
    public int? DefaultPageSize { get; init; }
    public int? MaxPageSize { get; init; }
}
```

## Parameters

| Name | Type | Description |
|------|------|-------------|
| `DefaultPageSize` | `int?` | Default page size when `FilterRequest.PageSize` is `null`. `null` inherits from `[FilterDefaults]`. |
| `MaxPageSize` | `int?` | Inclusive upper bound on `FilterRequest.PageSize`. Requests exceeding this fail validation with code `PageSizeTooLarge`. `null` inherits from `[FilterDefaults]`. |

## Examples

```csharp
[GenerateFilter<Report>]
[PageSettings(DefaultPageSize = 100, MaxPageSize = 500)]
public partial class ReportFilter
{
    // ...
}
```

## Related diagnostics

No FN-rule directly targets `[PageSettings]`. Misuse surfaces at runtime through [`FilterValidationCode.PageSizeTooLarge`](../types/filter-validation-code.md).

## See also

- [Page settings](../../guides/page-settings.md)
- [`[FilterDefaults]`](filter-defaults.md)
- [`PageResult<T>`](../types/page-result.md)
