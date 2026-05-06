---
title: Installation
description: Add the three Filtering.Net NuGet packages to your .NET project.
---

# Installation

## Required packages

```bash
dotnet add package Filtering.Net
dotnet add package Filtering.Net.Generator
dotnet add package Filtering.Net.EntityFrameworkCore
```

- **`Filtering.Net`** ships the runtime types: `FilterRequest`, `FilterNode`, `IFilterDefinition<T>`, `FilterValidationException`, the attributes (`[GenerateFilter<T>]`, `[Map]`, `[FilterProfile<T>]`, …), the built-in profiles, and the synchronous `IQueryable<T>.Apply(...)` extension.
- **`Filtering.Net.Generator`** ships the Roslyn incremental source generator and its 24-rule analyzer (`FN0001`–`FN0016` errors, `FN1001`–`FN1008` warnings). It is analyzer-only — no runtime DLL — and is consumed as an analyzer reference at compile time.
- **`Filtering.Net.EntityFrameworkCore`** ships the EF Core async helpers: `IQueryable<T>.ApplyPagedAsync(...)` and `PageResult<T>`. Install it when your call site is an EF Core controller or handler. If you only need synchronous `Apply(...)` against an in-memory `IQueryable<T>`, you can skip this package.

## Target framework support

| Package | Targets |
|---------|---------|
| `Filtering.Net` | `netstandard2.0` |
| `Filtering.Net.Generator` | `netstandard2.0` (analyzer-only, no runtime DLL) |
| `Filtering.Net.EntityFrameworkCore` | `net8.0`, `net9.0`, `net10.0` |

The runtime stays on `netstandard2.0` so it can be loaded inside the analyzer process and on every consumer TFM. EF Core 8.0.13 / 9.0.5 / 10.0.0 floors are pinned per TFM in the EF helpers package (the 8.0 / 9.0 patches close GHSA-qj66-m88j-hmgj on transitive `Microsoft.Extensions.Caching.Memory`).

## Verify the install

After adding the packages, run:

```bash
dotnet build
```

A clean install on an empty project should succeed with zero diagnostics. The generator only emits source for assemblies that contain at least one `[GenerateFilter<T>]` declaration, so an empty project is a valid no-op.

!!! tip
    `TreatWarningsAsErrors` is recommended on consumer projects too — the analyzer's `FN1xxx` warnings catch translatability problems that would otherwise become runtime EF Core errors.

## See also

- [Your first filter](your-first-filter.md)
- [DI registration](di-registration.md)
- [How it works](../concepts/how-it-works.md)
