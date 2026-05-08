# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `[MapNested]` and `[MapNested<TFilter>]` attributes for compile-time filter inlining via navigation properties. The generator splices another `[GenerateFilter<TNav>]` partial's `[Map]` mappings (with `Sortable`, `Alias`, `Only`/`Except`, custom `[FilterOperator]` bodies) into the host filter under a dotted prefix at compile time. Reference navigations only in v1; collection navigations and `[InterceptValue]` / `[PropertyMap]` splice-through are deferred. New error diagnostics `FN0016 NestedCycle`, `FN0017 NestedCrossAssembly`, `FN0018 NestedAmbiguous`, `FN0019 NestedTargetNotFound`, `FN0020 NestedNavigationInvalid`, `FN0021 NestedCollectionUnsupported`. `FN0001 DuplicateMapping` broadened to cover any duplicate effective dotted path across `[Map]`, `[PropertyMap]`, and `[MapNested]`, with multi-source-list message and `additionalLocations` reporting.

### Removed
- `FN0002 DuplicateSortable` analyzer rule. It was a value-aware refinement of `FN0001` that didn't change the user's required action; both cases now fold into `FN0001 DuplicateMapping`. Suppressions like `dotnet_diagnostic.FN0002.severity = none` become silent no-ops; remove them at your convenience.

### Changed
- **Analyzer diagnostic IDs renumbered** after the FN0002 removal. Errors `FN0003`–`FN0022` shift down by one to `FN0002`–`FN0021`. Mapping: `FN0003 MapAndPropertyMapBoth` → `FN0002`, `FN0004 PropertyNotFound` → `FN0003`, `FN0005 IncompatibleProfile` → `FN0004`, `FN0006 UnknownOperator` → `FN0005`, `FN0007 MissingPartial` → `FN0006`, `FN0008 NoInferableProfile` → `FN0007`, `FN0009 DuplicateInterceptor` → `FN0008`, `FN0010 NonStaticOperator` → `FN0009`, `FN0011 AliasCollision` → `FN0010`, `FN0012 InvalidBaseProfile` → `FN0011`, `FN0013 InterceptorWithoutMap` → `FN0012`, `FN0014 AmbiguousProfile` → `FN0013`, `FN0015 ProfileMissingExtractor` → `FN0014`, `FN0016 DuplicateOperatorOnProfile` → `FN0015`, `FN0017 NestedCycle` → `FN0016`, `FN0018 NestedCrossAssembly` → `FN0017`, `FN0019 NestedAmbiguous` → `FN0018`, `FN0020 NestedTargetNotFound` → `FN0019`, `FN0021 NestedNavigationInvalid` → `FN0020`, `FN0022 NestedCollectionUnsupported` → `FN0021`. Warnings (`FN1001`–`FN1008`) are unchanged. Final analyzer surface: 21 errors + 8 warnings = 29 rules.
- Project-wide diagnostic location enrichment: every analyzer rule that has multi-site context now reports `additionalLocations` so the IDE Error List, Rider Inspections panel, and `dotnet build` output surface every relevant site (e.g. both `[Map]` declarations on a duplicate-mapping error). See the diagnostics catalogue for the per-rule audit.

## [0.1.2] - 2026-05-07

### Removed
- `[ConvertWith<TConverter>]` attribute and the `FN0007 InvalidValueConverter` diagnostic. EF Core value converters are configured on the model side via `HasConversion<>` in `OnModelCreating`; once registered, EF translates the predicate against the property's CLR type without any Filtering.Net hint. The attribute documented behavior it did not contribute to and carried a latent bug on the nullable-enum path. Consumers using `[ConvertWith<>]` should remove it — no replacement is needed.

### Changed
- **Analyzer diagnostic IDs renumbered to stay contiguous** after the FN0007 removal. Errors `FN0008`–`FN0017` shift down by one to `FN0007`–`FN0016`. Mapping: `FN0008 MissingPartial` → `FN0007`, `FN0009 NoInferableProfile` → `FN0008`, `FN0010 DuplicateInterceptor` → `FN0009`, `FN0011 NonStaticOperator` → `FN0010`, `FN0012 AliasCollision` → `FN0011`, `FN0013 InvalidBaseProfile` → `FN0012`, `FN0014 InterceptorWithoutMap` → `FN0013`, `FN0015 AmbiguousProfile` → `FN0014`, `FN0016 ProfileMissingExtractor` → `FN0015`, `FN0017 DuplicateOperatorOnProfile` → `FN0016`. Warnings (`FN1001`–`FN1008`) are unchanged. Final analyzer surface: 16 errors + 8 warnings = 24 rules.
- Every diagnostic's `helpLinkUri` now points at a single catalogue page (`https://sheva-serga.github.io/Filtering.NET/diagnostics/`) instead of per-rule explainer pages. Future renumbering is no longer a help-link breaking change.
- Documentation restructured: the `Reference` section (40+ per-attribute / per-type / per-rule pages) collapsed into a single `Diagnostics catalogue` table; mkdocs path moved from `docs/reference/diagnostics/` to `docs/diagnostics/`.

## [0.1.0] - 2026-05-05

### Added
- `Filtering.Net` runtime: `[GenerateFilter<T>]` attribute set, `FilterRequest` / `FilterNode` types, `IFilterDefinition<T>`, built-in profiles, `IQueryable.Apply`.
- `Filtering.Net.Generator`: Roslyn incremental source generator + 24-rule analyzer (FN0001–FN0016 errors, FN1001–FN1008 warnings).
- `Filtering.Net.EntityFrameworkCore`: `ApplyPagedAsync` + `PageResult<T>`; `net10.0` / EF Core 10 target alongside `net8.0` and `net9.0`.
- MkDocs documentation site under `docs/`.
- GitHub Actions workflow `nuget.yml` for tag-driven NuGet.org publishing of all three packages.

### Security
- Bumped EF Core floor versions to `8.0.13` / `9.0.5` / `10.0.0` to patch [GHSA-qj66-m88j-hmgj](https://github.com/advisories/GHSA-qj66-m88j-hmgj) (NU1903) in transitive dependencies.
