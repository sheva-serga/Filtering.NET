# CLAUDE.md — Filtering.Net solution

Source-generated filter / sort / page library for `IQueryable<T>` and EF Core. Consumers declare `[GenerateFilter<TEntity>]` partials with `[Map]`-decorated methods; an incremental Roslyn generator emits `IFilterDefinition<TEntity>` plus DI wiring at compile time.

## Solution layout

| Path | Role |
|------|------|
| `src/Filtering.Net/` | Runtime: attributes, request types (`FilterRequest`, `FilterNode`, `SortItem`), validation primitives, profile catalog, `IQueryable.Apply(...)` extension. `netstandard2.0`. |
| `src/Filtering.Net.Generator/` | Roslyn incremental source generator + analyzer. Templates live as embedded `.scriban` resources under `Emission/Templates/`. `netstandard2.0`. |
| `src/Filtering.Net.EntityFrameworkCore/` | EF async helpers (`ApplyPagedAsync`, `PageResult<T>`). Multi-targets `net8.0`/`net9.0`/`net10.0`. |
| `samples/UserManagement.WebApi/` | ASP.NET Core 9 + EF Core 9 + PostgreSQL end-to-end demo. |
| `tests/Filtering.Net.Tests/` | Runtime unit tests. |
| `tests/Filtering.Net.Generator.Tests/` | Generator extraction + emission + analyzer tests. Mix of compile-and-run, snapshot (`Verify.Xunit`), and runtime end-to-end. |
| `tests/Filtering.Net.EntityFrameworkCore.Tests/` | EF integration tests against SQLite + Testcontainers Postgres / SQL Server. |
| `docs/diagnostics/` | One markdown explainer per `FN0xxx` / `FN1xxx` rule. New diagnostics MUST add a file here. |

## Build / test commands

```sh
dotnet build              # whole solution
dotnet test               # 213 tests across 3 test projects
dotnet test tests/Filtering.Net.Generator.Tests --filter "FullyQualifiedName~CompositeValidate"
```

`TreatWarningsAsErrors` is on solution-wide. Build failures often surface as warning-promoted-to-error — read the actual message before suppressing.

## Conventions enforced across the solution

- **Variable names spell things out.** `validationErrors` not `errs`, `requestedPageSize` not `rps`. The codebase consistently uses meaningful names; matching that style is a hard requirement.
- **Custom exception types** — `FilterValidationException`, `FilterDispatchException`, `FilterConfigurationException`, `FilterEmissionException`. Never throw `InvalidOperationException` / `UnreachableException` from production code.
- **Composite interfaces over capability splits** — `IFilterDefinition<T>` carries every Validate/ApplyFilter/ApplySorting overload; we don't split it into `IValidator` + `IPredicateBuilder` etc.
- **Drop features over adding magic defaults** — when a config combination is ambiguous, the design surfaces a diagnostic rather than guessing.
- **One method per concern in the public DSL** — e.g. `For(...)` vs `.Operator(...)` are distinct steps; we don't overload a single `Configure` method that accepts both.

## Source generator architecture

- **Pipeline branches** in `FilterGenerator.cs`: branch 1 walks `[GenerateFilter<TEntity>]` partials → emits filter classes; branch 2 walks `[FilterProfile<T>]` classes → emits per-profile diagnostics. Cross-pipeline diagnostics (FN1003 / FN1004) join both `.Collect()` outputs.
- **Model extraction** is in `ModelExtraction/` and produces `EquatableList<T>`-based records so the Roslyn cache can deduplicate compilations cheaply.
- **Emission** uses Scriban templates source-embedded into the analyzer DLL (`PackageScribanIncludeSource`). Each emitter exposes `BuildView(model) → record` plus `Emit(model) → string` that delegates to `ScribanRuntime.Render`. `SourceEmitter.cs` is the orchestrator that composes child emitter outputs into the top-level `FilterClass.scriban`.
- **Analyzer rules** are catalogued in `Diagnostics/DiagnosticDescriptors.cs`. Errors are `FN0001`–`FN0017`, warnings are `FN1001`–`FN1008`. Every rule has a sibling `docs/diagnostics/FNxxxx.md` explainer; the descriptor's `helpLinkUri` points at it.

## Snapshot-test workflow

`tests/Filtering.Net.Generator.Tests/Emission/Snapshots/` holds `.verified.cs` baselines under `Verify.Xunit`. When a generator change intentionally alters emitted output:

1. `dotnet test` — failing tests write `.received.cs` siblings.
2. Inspect each `.verified.cs` vs `.received.cs` diff and confirm semantic equivalence (`diff -uw` ignores whitespace; non-whitespace differences must be traced to the template/emitter change you intended).
3. Bless: `Get-ChildItem -Recurse -Filter '*.received.cs' | ForEach-Object { Move-Item -Force $_ ($_ -replace '\.received\.cs$', '.verified.cs') }` (PowerShell) — equivalent `find … -exec mv … \;` on bash.
4. Re-run `dotnet test` to confirm green.

Compile-and-run and runtime-behaviour tests (`*_Compiles`, `EmittedCodeCompilesTests`, `EndToEndRuntimeTests`) MUST stay green at every step — only snapshot diffs may go red mid-refactor.

## Adding a new diagnostic

1. Add a `DiagnosticDescriptor` to `DiagnosticDescriptors.cs` with the next `FN0xxx` / `FN1xxx` id.
2. Create `docs/diagnostics/FNxxxx.md` — title, what triggers it, how to fix.
3. Wire it into the relevant extractor / analyzer.
4. Add a `tests/Filtering.Net.Generator.Tests/.../*.cs` test that asserts the diagnostic fires (and doesn't fire when the offending construct is removed).

## Worktrees

`.worktrees/` is gitignored; use `git worktree add .worktrees/<feature> -b feature/<feature>` for parallel branches that need isolation. The `superpowers:using-git-worktrees` workflow handles cleanup.
