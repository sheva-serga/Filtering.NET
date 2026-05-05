# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-05-05

### Added
- `Filtering.Net` runtime: `[GenerateFilter<T>]` attribute set, `FilterRequest` / `FilterNode` types, `IFilterDefinition<T>`, built-in profiles, `IQueryable.Apply`.
- `Filtering.Net.Generator`: Roslyn incremental source generator + 25-rule analyzer (FN0001–FN0017 errors, FN1001–FN1008 warnings).
- `Filtering.Net.EntityFrameworkCore`: `ApplyPagedAsync` + `PageResult<T>`; `net10.0` / EF Core 10 target alongside `net8.0` and `net9.0`.
- MkDocs documentation site under `docs/`.
- GitHub Actions workflow `nuget.yml` for tag-driven NuGet.org publishing of all three packages.

### Security
- Bumped EF Core floor versions to `8.0.13` / `9.0.5` / `10.0.0` to patch [GHSA-qj66-m88j-hmgj](https://github.com/advisories/GHSA-qj66-m88j-hmgj) (NU1903) in transitive dependencies.
