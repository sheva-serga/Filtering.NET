# CLAUDE.md — Filtering.Net (runtime)

The runtime contract that source-generated filter classes implement against. No reflection, no expression-tree building at request time — every typed predicate is generator-emitted ahead of time.

**Target:** `netstandard2.0`. Polyfilled via `PolySharp`. Ships as the `Filtering.Net` NuGet package.

## What lives here

| Folder | Contents |
|--------|----------|
| `Attributes/` | `[GenerateFilter<T>]`, `[Map]`, `[PropertyMap]`, `[FilterProfile<T>]`, `[FilterOperator]`, `[FilterValidator]`, `[InterceptValue]`, `[ConvertWith<TConverter>]`, `[FilterDefaults]`, `[PageSettings]`. The generator reads these; consumers stick them on partials. |
| `Requests/` | `FilterRequest`, `FilterNode` + subtypes (`FilterGroup`, `FilterLeaf`), `SortItem`, `SortDir`, `LogicalOp`, plus the polymorphic `FilterNodeJsonConverter`. |
| `Validation/` | `FilterValidationResult`, `FilterValidationError`, `FilterValidationCode`, plus runtime helpers: `LeafValidation` (per-leaf error shaping) and `PageValidation` (page/pageSize bounds). Generated code calls these helpers — the call surface is part of the runtime API contract. |
| `Profiles/` | Built-in profiles: `StringFilter`, `BoolFilter`, `GuidFilter`, `DateTimeFilter`, `Numeric/*`, `Temporal/*`, `Enum/*`. Each exposes `TryGet*` extractors that the generator calls when resolving a property's profile. |
| `Override/` | DSL types for `[PropertyMap]` overrides: `FilterRule<TEntity, TColumn>`, `FilterRuleBuilder<TEntity, TColumn>`. The generator parses the syntax tree of `For(...).Operator(...)` chains; the runtime types exist so consumer code compiles. |
| `Composition/` | `PredicateBuilder.AndAlso/OrElse/Not` — the runtime helpers used by emitted `CombineGroup` to glue child predicates together. |
| `Interception/` | `InterceptContext` passed to `[InterceptValue]`-decorated methods. |
| `Exceptions/` | `FilteringException` (base), `FilterValidationException`, `FilterDispatchException`, `FilterConfigurationException`. |
| `IFilterDefinition.cs` | Composite interface every emitted filter class implements: `Validate(FilterRequest)`, `Validate(FilterNode?)`, `Validate(IReadOnlyList<SortItem>?)`, `Validate(int?, int?)`, `ApplyFilter(IQueryable<T>, FilterNode?)`, `ApplySorting(IQueryable<T>, sort, page, pageSize)`. |
| `FilteringQueryableExtensions.cs` | `IQueryable<T>.Apply(IFilterDefinition<T>, FilterRequest)` — the synchronous `validate-then-filter-then-sort-then-page` orchestrator. EF async sibling lives in `Filtering.Net.EntityFrameworkCore`. |

## Editing rules

- **Public API contract.** Anything `public` is consumed by either the generator's emitted code or external consumers. Renames and signature changes require updating `Emission/Templates/*.scriban` and re-blessing snapshots.
- **`LeafValidation` / `PageValidation` helper additions** — when adding a new validation shape, prefer extending these helper classes (and emitting a one-line forwarder) over inlining new logic into the template. Reduces emitted code per filter class and centralises the rule.
- **Built-in profile extractors** are called by name from the generator (`ProfileExtractorEmitter.EmitScalarCall` / `EmitArrayCall`). Adding an operator to a built-in profile means adding the `TryGet*` method here AND wiring it into `BuiltInProfileCatalog.cs` in the generator.
- **`netstandard2.0` constraint** keeps this assembly loadable inside the analyzer process and on every consumer TFM. Don't take dependencies on `net*`-only APIs without checking PolySharp can polyfill them.

## Profile system

A *profile* is a static class decorated with `[FilterProfile<T>]` that names a set of operators. Built-in profiles ship here (`StringFilter`, etc.). Consumers can declare their own with `BasedOn = typeof(StringFilter)` to inherit operators and add `[FilterOperator("name")]` extensions; the generator inlines those custom operator bodies into the per-property `Build` method.

The `[Map(..., Profile = typeof(MyProfile))]` attribute selects which profile a property uses. When `Profile` is omitted, the resolver in the generator (`ProfileResolver`) picks a built-in by CLR type — `string` → `StringFilter`, `int`/`long`/… → `Numeric/*`, `DateTime` → `DateTimeFilter`, enums → an auto-emitted per-enum profile.

## Tests for this assembly

`tests/Filtering.Net.Tests/` covers:
- Validation primitives (`Validation/`)
- Request JSON round-tripping (`Requests/`)
- `PredicateBuilder` composition (`Composition/`)
- Profile extractor behaviours (`Profiles/`)
- The `Apply` extension's orchestration of validate → filter → sort → page (`FilteringQueryableExtensionsTests.cs`)

These do NOT exercise the source generator end-to-end — that's covered in `tests/Filtering.Net.Generator.Tests/Emission/`.
