# CLAUDE.md — Filtering.Net.Generator

Roslyn incremental source generator + analyzer. Walks `[GenerateFilter<TEntity>]` partial classes and `[FilterProfile<T>]` profile classes, emits typed `IFilterDefinition<TEntity>` implementations and an assembly-wide `AddFiltering` DI extension.

**Target:** `netstandard2.0` (loaded into the analyzer process). Ships as the `Filtering.Net.Generator` analyzer-only NuGet package — no runtime dependency.

## Two-pipeline architecture

`FilterGenerator.cs` registers two `ForAttributeWithMetadataName` pipelines:

1. **`[GenerateFilter<TEntity>]` branch** — extracts a `FilterClassModel`, reports per-class diagnostics, emits one source file per class via `SourceEmitter.EmitForClass`. A collected view drives the assembly-wide `AddFiltering()` extension and the per-enum auto-emitted profiles (`EnumProfileEmitter`).
2. **`[FilterProfile<T>]` branch** — extracts profile-level models and reports per-profile diagnostics (`FN0006`, `FN0008`, `FN0013`, `FN1001`, `FN1002`, …).

Cross-pipeline diagnostics (`FN1003 ProfileUnused`, `FN1004 OperatorUnused`) join both `.Collect()` outputs.

## Folder layout

| Folder | Role |
|--------|------|
| `Discovery/` | `EnumTypeCollector` — finds enums referenced anywhere in the filter class graph so the per-enum profile auto-emitter can emit one `[FilterProfile<TEnum>]` per. |
| `ModelExtraction/` | `FilterClassExtractor`, `PropertyMappingExtractor`, `PropertyMapOverrideExtractor`, `ProfileExtractor`, `ProfileResolver`, `ProfileIndex(Builder)`. Pure-functional Roslyn-symbol-walkers that emit `EquatableList<T>`-based records. Models in `Models/`. |
| `Models/` | Equatable record shapes used between extraction and emission. `FilterClassModel` is the top-level model. `EquatableList<T>` is the deduplication-friendly list type — use it everywhere a model carries a sequence. |
| `Diagnostics/` | `DiagnosticDescriptors.cs` — every `FN0xxx` / `FN1xxx` registration. Every rule's `helpLinkUri` points at the single catalogue page (`https://sheva-serga.github.io/Filtering.NET/diagnostics/`), source at `docs/github/docs/diagnostics/index.md`. |
| `Emission/` | All output. `*Emitter.cs` files own a slice of the generated file. Each exposes `BuildView(model) → record` and `Emit(model) → string` that delegates to a single `ScribanRuntime.Render` call. |
| `Emission/Templates/` | `.scriban` templates. Embedded as `<EmbeddedResource>`. Each template's logical name maps to the resource `Filtering.Net.Generator.Emission.Templates.{Name}.scriban`. |
| `Emission/Views/` | Per-template view-model records (PascalCase here, snake_case inside templates via Scriban's `StandardMemberRenamer` default). |

## Emission contract

```
SourceEmitter.EmitForClass(FilterClassModel)
  → BuildView()                                 (pure C# composition)
  → ScribanRuntime.Render("FilterClass", view)  (Scriban renders the top-level template)
       references each child emitter's pre-rendered string in its view fields
       (validate_node_body, apply_filter_body, per_property_class_bodies, …)
```

Each child emitter:
- Owns one `.scriban` template + one view-model record under `Views/`.
- Returns a string; never writes directly to a builder.
- Heavy logic stays in C# — operator-shape grouping (`OperatorShapeGrouping.cs`), profile lookup (`BuiltInProfileCatalog.cs`), lambda body rewriting (`CustomOperatorEmitter.cs`), value-shape resolution (`PropertyValueShape.cs`). Templates only loop and conditionally render.

The `to_operator_id` Scriban filter is registered by `ScribanRuntime.Render`; it forwards to `EmissionNames.OperatorIdentifier` so templates can map `"isNull"` → `IsNull`.

## Editing emitters

When changing what the generator emits:

1. Update the `.scriban` template (or add a new one).
2. Update the view-model record under `Views/` — fields are PascalCase; templates reference them as snake_case.
3. Update the `*Emitter.cs` `BuildView` to populate the new fields.
4. `dotnet test --filter "FullyQualifiedName~CompileEmittedCode"` MUST stay green — this is the regression net.
5. Snapshot tests under `Emission/Snapshots/` will need re-blessing — see the root `CLAUDE.md` for the workflow.

The Scriban runtime is source-embedded (`PackageScribanIncludeSource`), so the analyzer DLL has zero NuGet dependencies at consumer-build time. Don't take new package references without considering the source-embedding path.

## csproj quirks worth remembering

- `<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>` — RS1xxx Roslyn-API-restriction rules apply.
- `<PackageScribanIncludeSource>true</PackageScribanIncludeSource>` — Scriban is built from source into this assembly; we suppress NU190x security warnings on the Scriban package because no Scriban DLL ships at runtime.
- `<NoWarn>` includes RS1032 (we like terminal periods on diagnostic messages), CS1591/CS1573/CS1574 (internals are intentionally undocumented), NU190x (Scriban source-embedding mitigates).
- `<PolySharpExcludeGeneratedTypes>System.Runtime.CompilerServices.ModuleInitializerAttribute</PolySharpExcludeGeneratedTypes>` — avoids CS0433 when test project (net9.0) sees both polyfill and BCL definition.

## Adding a new emitter

1. New `*Emitter.cs` in `Emission/` with `Emit(model) → string` and `BuildView` returning a record from `Views/`.
2. New `Views/{Name}View.cs` record.
3. New `Templates/{Name}.scriban` (will be picked up by the `<EmbeddedResource Include="...\*.scriban" />` glob automatically).
4. Wire it into `SourceEmitter.BuildView` if it composes into the per-class output, or into `FilterGenerator.RegisterSourceOutput` if it's a separate emission target (like the DI extension or per-enum profiles).
5. Add a snapshot test under `tests/Filtering.Net.Generator.Tests/Emission/`.
