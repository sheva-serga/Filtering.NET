# CLAUDE.md — Filtering.Net (runtime)

The runtime: request types, profiles, and the filter engine. Generated filter classes derive from `FilterDefinition<TEntity>` and only supply a schema. The engine composes predicates per request from typed, compiler-checked lambdas. No reflection over consumer types, no `MakeGenericMethod`, no `Compile()`.

**Target:** `netstandard2.0;net8.0;net9.0;net10.0`. The netstandard2.0 asset is polyfilled via `PolySharp` and carries the `System.Text.Json` package reference; the net8.0+ assets need neither and are the only ones that compile `Profiles/Temporal/DateOnlyFilter.cs` / `TimeOnlyFilter.cs` (both `#if NET6_0_OR_GREATER`). Ships as the `Filtering.Net` NuGet package.

## What lives here

| Folder | Contents |
|--------|----------|
| `Attributes/` | `[GenerateFilter<T>]`, `[Map]`, `[MapNested]`, `[PropertyMap]`, `[FilterProfile<T>]`, `[FilterOperator]`, `[InterceptValue]`, `[FilterDefaults]`, `[PageSettings]`. The generator reads these; consumers stick them on partials. |
| `Requests/` | `FilterRequest`, `FilterNode` + subtypes (`FilterGroup`, `FilterLeaf`), `SortItem` (nullable `Dir`), `SortDir`, `LogicalOp`, plus the polymorphic `FilterNodeJsonConverter`. |
| `Profiles/` | Built-in profile static classes (`StringFilter`, `Numeric/*`, `Temporal/*`, …). Each keeps its `[FilterOperator]` templates and `TryGet*` parsers and exposes a runtime `Profile` built from them. Also `FilterProfile<TColumn>`, `FilterOperator` factories, and the internal `ValueOperator` / `UnaryOperator`. |
| `Schema/` | `FilterProperty<TEntity>` + `FilterProperty` factories, `FilterPropertyBuilder`, `FilterSchema<TEntity>`, `FilterSchemaBuilder` (incl. `AddNested`), `FilterNestingContext`, `FilterSettings`. `ColumnFilterProperty` is the one concrete property type. |
| `Engine/` | Internal: `ExpressionSplicer`, `NullableColumnLifter`, `NullableColumnSupport`, `FilterValueHolder`, `BoundOperator`, `FilterTreeValidator`, `FilterPredicateComposer`. |
| `FilterDefinition.cs` | The engine's public face: implements `IFilterDefinition<TEntity>` over a schema. |
| `Override/` | `FilterRule` / `FilterRuleBuilder` for `[PropertyMap]`. The builder is real: the generated filter calls the consumer's method once at construction. |
| `Validation/` | `FilterValidationResult`, `FilterValidationError`, `FilterValidationCode` (public); `LeafValidation`, `PageValidation` (internal error shaping). |
| `Composition/` | `PredicateBuilder.AndAlso/OrElse/Not`, used by `FilterPredicateComposer`. |
| `Interception/` | `InterceptContext` passed to `[InterceptValue]` methods. |
| `Exceptions/` | `FilteringException` (base), `FilterValidationException`, `FilterDispatchException`, `FilterConfigurationException`. |
| `FilteringQueryableExtensions.cs` | `IQueryable<T>.Apply(IFilterDefinition<T>, FilterRequest)`: validate, filter, sort, page. |

## How a predicate is built

1. At property construction each allowed operator is *bound*: its column parameter is replaced by the property's accessor body. Unary operators are finished at this point.
2. Per request the parsed value is wrapped in `FilterValueHolder<T>` and spliced in as a member access on a constant. Query providers parameterize that shape; a bare `ConstantExpression` would be inlined into SQL.
3. `MapNullable` properties go through `NullableColumnLifter`, which rebuilds comparisons as lifted comparisons (`liftToNull: false`) and re-targets `values.Contains(column)` to the nullable array form. That reproduces what the C# compiler emits for `entity.NullableColumn == value`. Anything else — `column.Year`, `column % 2`, a second use of the value array alongside `Contains` — falls back to unwrapping the accessor to `TColumn`, and the whole lifted body is then wrapped in `accessor.HasValue && …` so null rows are excluded instead of throwing in memory (`IS NOT NULL` for providers). The re-targeting is dropped entirely when the predicate also reads the value array outside the matched `Contains`, so no free parameter can survive.

## Circular nesting

`FilterNestingContext` is the path of nestings a schema is being built through, each identified by the nesting key the generator emits: `<declaring filter class FQN>.<navigation property>`, plus `@<prefix>` when `Prefix` differs from the navigation name. `AddNested` enters a nesting only while its `MaxDepth` is not used up on that path, and throws `FilterConfigurationException` when an unbounded nesting repeats with no bounded nesting in between. `NestedFilterResolver` in the generator applies the same two rules at compile time (FN0014); keep them in step.

## Editing rules

- **Public API contract.** Generated code calls `FilterProperty.Map/MapNullable/MapRule`, `FilterPropertyBuilder`, `FilterSchemaBuilder` (`Add`, `AddNested`), `FilterNestingContext.Root`, `FilterSchema.LiftInto`, `FilterProfile.Create/Extend/ExtendWithOverrides`, `FilterOperator.*`, and each built-in's `Profile`. Renames require updating `Emission/` in the generator and re-blessing snapshots.
- **Validation paths, codes, and messages are pinned** by end-to-end tests. Change them deliberately.
- **Adding an operator to a built-in profile** means adding the `[FilterOperator]` template and listing it in that class's `Profile` initializer. `FilterProfileTests` fails if the two drift.
- **Value extraction rule.** Operators on built-in and auto-emitted enum profiles parse through `TryGetValue` / `TryGetArray`. Value operators declared on user profiles, and every `[PropertyMap]` value operator, deserialize through System.Text.Json with the definition's resolver.
- **`netstandard2.0` constraint** keeps this assembly loadable on every consumer TFM. Check PolySharp before using newer APIs.

## Tests for this assembly

`tests/Filtering.Net.Tests/`: `Engine/` covers the definition, properties, profiles, nullable lifting, and the rule builder through the public API with in-memory queries. The other folders cover request JSON, validation result types, `PredicateBuilder`, and profile parsers. Generator end-to-end coverage lives in `tests/Filtering.Net.Generator.Tests/Emission/`.
