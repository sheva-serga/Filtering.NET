# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
