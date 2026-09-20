# CLAUDE.md — Filtering.Net.Generator

Roslyn incremental source generator + analyzer. Walks `[GenerateFilter<TEntity>]` partial classes and `[FilterProfile<T>]` profile classes. Emits a *schema* per filter class over the runtime `FilterDefinition<TEntity>` engine, runtime instances for user-declared profiles, per-enum profiles, and an assembly-wide `AddFiltering` DI extension. The generator never emits filtering logic and never copies consumer lambda bodies.

**Target:** `netstandard2.0` (loaded into the analyzer process). Ships as the `Filtering.Net.Generator` analyzer-only NuGet package.

## Two-pipeline architecture

`FilterGenerator.cs` registers two `ForAttributeWithMetadataName` pipelines:

1. **`[GenerateFilter<TEntity>]` branch** — extracts a `FilterClassModel`, reports per-class diagnostics, runs `NestedFilterResolver`, emits one source file per class via `SourceEmitter.EmitForClass`. A collected view drives `AddFiltering()`, the per-enum profiles (`EnumProfileEmitter`), and `FilteringProfiles.g.cs` (`ProfileBridgeEmitter`).
2. **`[FilterProfile<T>]` branch** — extracts profile-level models and reports per-profile diagnostics.

Cross-pipeline diagnostics (`FN1003 ProfileUnused`, `FN1004 OperatorUnused`) join both `.Collect()` outputs.

## Folder layout

| Folder | Role |
|--------|------|
| `Discovery/` | `EnumTypeCollector` — finds enums referenced by filter classes so one `[FilterProfile<TEnum>]` is emitted per enum. |
| `ModelExtraction/` | `FilterClassExtractor`, `PropertyMappingExtractor`, `PropertyMapOverrideExtractor`, `MapNestedExtractor`, `NestedFilterResolver`, `ProfileExtractor`, `ProfileResolver`, `ProfileIndex(Builder)`, `ProfileBridgeBuilder`, `OperatorPredicateSignature`, `TypeNameFormatter`. Symbol walkers that emit `EquatableList<T>`-based records. |
| `Models/` | Equatable record shapes between extraction and emission. `FilterClassModel` is the top-level model. |
| `Diagnostics/` | `DiagnosticDescriptors.cs` — every `FN0xxx` / `FN1xxx` registration, all pointing at the single catalogue page. |
| `Emission/` | `SourceEmitter` (filter class), `ProfileBridgeEmitter`, `EnumProfileEmitter`, `DiExtensionEmitter`, `ScribanRuntime`. |
| `Emission/Templates/` | `FilterClass`, `ProfileBridge`, `EnumProfile`, `DiExtension` `.scriban` templates, embedded as resources. |
| `Emission/Views/` | Per-template view-model records (PascalCase here, snake_case inside templates). |

## What each piece decides

- **`PropertyMappingExtractor`** resolves the profile, the allowed operators, `IsNullableValueType` (chooses `Map` vs `MapNullable`), and the chain of `ProfileBridgeModel`s the property's profile needs.
- **`ProfileBridgeBuilder`** reads operator shapes from the member's declared type (`Expression<Func<TColumn, bool>>` or `Expression<Func<TColumn, TValue, bool>>`), not from syntax. Value operators on user profiles always use the typed-value (JSON resolver) factory. Built-in and enum profiles are referenced through their own `Profile` accessor and get no bridge.
- **`PropertyMapOverrideExtractor`** reads `.Operator(...)` calls only to learn operator names and value types, for FN1008 and for typed-value detection. Arity of the lambda decides unary vs value, because an untyped two-parameter lambda may fail to bind.
- **`NestedFilterResolver`** still merges the nested filter's mappings into the host model. That merged list exists for diagnostics (FN0001 duplicates, cycles) and typed-value propagation. Emission uses only the host's own properties (`SourceFilterClassFqn is null`) plus `NestedMappings[*].ResolvedTargetClassFqn`; the actual lifting happens at runtime through `FilterSchemaBuilder.AddNested`. The DFS keeps a path of `(class, nesting key, bounded?)` steps: a nesting with `MaxDepth > 0` stops contributing once it was entered that many times on the path, and re-entering a class is FN0015 only when no nesting since its previous visit is bounded. This mirrors `FilterNestingContext` in the runtime; the nesting key is `<declaring class FQN>.<method name>` on both sides.
- **`HasAnyTypedValueProperty`** gates the resolver-accepting constructor pair. It is true when the class, or any filter it nests (including that filter's `[PropertyMap]` rules), has a typed-value operator.

## Emission contract

```
SourceEmitter.EmitForClass(FilterClassModel)
  → BuildView()                                 one string per schema entry, formatted in C#
  → ScribanRuntime.Render("FilterClass", view)  base class, ctor(s), CreateSchema
```

Templates only loop and branch. Scriban re-indents multi-line values to the column of the tag, so continuation indents in C# are relative, not absolute. Emitted files use `#nullable enable annotations` so consumer nullability mismatches never become warnings in generated code.

## Editing emitters

1. Update the `.scriban` template and its view record under `Views/`.
2. Update the emitter's `BuildView`.
3. `dotnet test --filter "FullyQualifiedName~Compiles|FullyQualifiedName~EndToEnd"` MUST stay green.
4. Re-bless snapshots under `Emission/Snapshots/` after inspecting the diffs. See the root `CLAUDE.md`.

The Scriban runtime is source-embedded (`PackageScribanIncludeSource`), so the analyzer DLL has zero NuGet dependencies at consumer-build time.

## csproj quirks worth remembering

- `<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>` — RS1xxx Roslyn-API-restriction rules apply.
- `<PackageScribanIncludeSource>true</PackageScribanIncludeSource>` — Scriban is built from source into this assembly; NU190x warnings on the Scriban package are suppressed because no Scriban DLL ships.
- `<NoWarn>` includes RS1032, CS1591/CS1573/CS1574 (internals are intentionally undocumented), NU190x.
- `<PolySharpExcludeGeneratedTypes>System.Runtime.CompilerServices.ModuleInitializerAttribute</PolySharpExcludeGeneratedTypes>` — avoids CS0433 in the net9.0 test project.
