# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-20

The filter engine moved from generated code into the runtime. The generator now emits a small typed schema per filter class, and `FilterDefinition<TEntity>` validates requests, composes predicates, and applies sorting for every filter. A filter with two string properties went from 347 emitted lines to about 25. `[Map]` and `[MapNested]` moved from marker methods onto the filter class, so a filter no longer needs any empty partial methods.

### Changed
- **Breaking:** `[Map]`, `[MapNested]`, and `[MapNested<TFilter>]` are class-level attributes (`AllowMultiple`). Move each one from its `private static partial void MapX();` method onto the `[GenerateFilter<T>]` class and delete the method. Leaving one on a method is now a C# compiler error (CS0592). `[InterceptValue]` and `[PropertyMap]` stay on methods, because those methods have bodies.
- **Breaking: analyzer ids renumbered once for this release.** Two rules were removed (`DuplicateSortable`, and `MissingPartial`, which cannot occur without marker methods), and the ids stay contiguous. Mapping from 0.1.2: `FN0003 MapAndPropertyMapBoth` → `FN0002`, `FN0004 PropertyNotFound` → `FN0003`, `FN0005 IncompatibleProfile` → `FN0004`, `FN0006 UnknownOperator` → `FN0005`, `FN0008 NoInferableProfile` → `FN0006`, `FN0009 DuplicateInterceptor` → `FN0007`, `FN0010 NonStaticOperator` → `FN0008`, `FN0011 AliasCollision` → `FN0009`, `FN0012 InvalidBaseProfile` → `FN0010`, `FN0013 InterceptorWithoutMap` → `FN0011`, `FN0014 AmbiguousProfile` → `FN0012`, `FN0015 ProfileMissingExtractor` → `FN0013`, `FN0016 DuplicateOperatorOnProfile` → `FN0014`. New in this release: `FN0015 NestedCycle`, `FN0016 NestedCrossAssembly`, `FN0017 NestedAmbiguous`, `FN0018 NestedTargetNotFound`, `FN0019 NestedNavigationInvalid`, `FN0020 NestedCollectionUnsupported`, `FN0021 FilterClassHasBaseType`, `FN0022 NestedMaxDepthInvalid`. Warnings (`FN1001`–`FN1008`) are unchanged. Final surface: 22 errors + 8 warnings = 30 rules.
- **Breaking:** generated filter classes derive from `FilterDefinition<TEntity>`. A `[GenerateFilter]` partial can no longer declare its own base class; new error `FN0021 FilterClassHasBaseType` reports it.
- **Breaking:** `SortItem.Dir` is now `SortDir?`. An omitted direction uses the property's `DefaultSortDirection`, which was previously ignored.
- **Breaking:** `MaxNestingDepth` and `MaxLeafConditions` from `[assembly: FilterDefaults]` are now enforced during validation (`NestingTooDeep`, `TooManyConditions`). They were declared but never checked. Defaults are 10 and 50.
- **Breaking:** `LeafValidation` and `PageValidation` are internal. They were public only because generated code called them.
- **Breaking:** `FilterRule<TEntity, TValue>.Operators` is now `IReadOnlyList<FilterOperator<TValue>>`.
- Predicates are composed at request time from compiler-checked accessor and operator lambdas, by parameter substitution. There is still no reflection over consumer types and no `Compile()`; trim and Native AOT behaviour is unchanged.
- `[MapNested]` reuses the nested filter's schema at runtime instead of copying its mappings into the host. `[InterceptValue]` methods and `[PropertyMap]` rules on the nested filter now carry through, which removes the corresponding 0.1.x limitation.
- `[InterceptValue]` and `[PropertyMap]` methods may be `private`.
- `[PropertyMap]` methods run once when the filter is constructed. `FilterRuleBuilder` is a real builder rather than a compile-time marker.
- `[InterceptValue]` with an array parameter applies to array operators such as `in`, and `Raw = true` replaces scalar parsing. Both were documented but not wired before.
- Project-wide diagnostic location enrichment: every analyzer rule that has multi-site context now reports `additionalLocations` so the IDE Error List, Rider Inspections panel, and `dotnet build` output surface every relevant site (e.g. both `[Map]` declarations on a duplicate-mapping error). See the diagnostics catalogue for the per-rule audit.

### Added
- `MaxDepth` on `[MapNested]` and `[MapNested<TFilter>]`. A self-referencing or circular filter graph (`Employee.Manager`, or `User.Department` with `Department.Head`) is now allowed when at least one nesting in the cycle declares `MaxDepth`; that nesting is followed at most that many times along one path. Cycles with no `MaxDepth` still raise `FN0015`. New error `FN0022 NestedMaxDepthInvalid` for a negative value. Runtime counterpart: `FilterSchemaBuilder.AddNested` and `FilterNestingContext`, which also turn an unbounded cycle in a hand-built schema into a `FilterConfigurationException`.
- Public runtime API for building definitions without the generator: `FilterDefinition<TEntity>`, `FilterSchema<TEntity>`, `FilterSchemaBuilder<TEntity>`, `FilterSettings`, `FilterProperty<TEntity>`, `FilterProperty.Map` / `MapNullable` / `MapRule`, `FilterPropertyBuilder<TEntity, TColumn>`, `FilterProfile<TColumn>`, `FilterOperator<TColumn>` and its factories, `TryParseValue<TValue>`.
- `Profile` accessor on every built-in profile, and on auto-emitted enum profiles.
- `FilterDefinition<TEntity>.Schema` for introspection of fields, aliases, operators, and sortable flags.
- `FilterRuleBuilder.Operator(name, predicate)` overload for operators that take no value.
- `FilteringProfiles.g.cs`: one emitted runtime `FilterProfile<T>` instance per custom profile referenced by the assembly's filters.
- `[MapNested]` and `[MapNested<TFilter>]` attributes for compile-time filter inlining via navigation properties. The generator splices another `[GenerateFilter<TNav>]` partial's `[Map]` mappings (with `Sortable`, `Alias`, `Only`/`Except`, custom `[FilterOperator]` bodies) into the host filter under a dotted prefix at compile time. Reference navigations only; collection navigations are deferred. New error diagnostics `FN0015` to `FN0020` (see the id mapping above). `FN0001 DuplicateMapping` broadened to cover any duplicate effective dotted path across `[Map]`, `[PropertyMap]`, and `[MapNested]`, with multi-source-list message and `additionalLocations` reporting.

### Removed
- `[FilterValidator]` attribute. The generator never read it.
- `MissingPartial` analyzer rule (`FN0007` in 0.1.2). `[Map]` no longer sits on a method, so it cannot occur.
- `DuplicateSortable` analyzer rule (`FN0002` in 0.1.2). It was a value-aware refinement of `FN0001` that didn't change the user's required action; both cases now fold into `FN0001 DuplicateMapping`. Suppressions like `dotnet_diagnostic.FN0002.severity = none` become silent no-ops; remove them at your convenience.

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
