# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `Filtering.Net` runtime: `[GenerateFilter<T>]` attribute set, `FilterRequest` / `FilterNode` types, `IFilterDefinition<T>`, built-in profiles, `IQueryable.Apply`.
- `Filtering.Net.Generator`: Roslyn incremental source generator + 25-rule analyzer (FN0001–FN0017 errors, FN1001–FN1008 warnings).
- `Filtering.Net.EntityFrameworkCore`: `ApplyPagedAsync` + `PageResult<T>`.
