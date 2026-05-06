---
title: Contributing
description: Build, test, and snapshot-bless workflow for repository contributors.
---

# Contributing

## Build and test

```sh
dotnet build              # whole solution
dotnet test               # all tests across the three test projects
dotnet test tests/Filtering.Net.Generator.Tests --filter "FullyQualifiedName~CompositeValidate"
```

`TreatWarningsAsErrors` is on solution-wide. Build failures often surface as warning-promoted-to-error — read the actual message before suppressing.

## Coding conventions

- **Variable names spell things out.** `validationErrors` not `errs`, `requestedPageSize` not `rps`. The codebase consistently uses meaningful names; matching that style is a hard requirement.
- **Custom exception types only** — `FilterValidationException`, `FilterDispatchException`, `FilterConfigurationException`, `FilterEmissionException`. Never throw `InvalidOperationException` / `UnreachableException` from production code.
- **Composite interfaces over capability splits** — `IFilterDefinition<T>` carries every Validate/ApplyFilter/ApplySorting overload; we don't split it into `IValidator` + `IPredicateBuilder` etc.
- **Drop features over adding magic defaults** — when a config combination is ambiguous, the design surfaces a diagnostic rather than guessing.
- **One method per concern in the public DSL** — e.g., `For(...)` and `.Operator(...)` are distinct steps; we don't overload a single `Configure` method that accepts both.

## Adding a new diagnostic

1. Add a `DiagnosticDescriptor` to `src/Filtering.Net.Generator/Diagnostics/DiagnosticDescriptors.cs` with the next `FN0xxx` / `FN1xxx` id.
2. Append a row to the appropriate table in `docs/github/docs/diagnostics/index.md` (errors or warnings).
3. Wire it into the relevant extractor / analyzer.
4. Add a test under `tests/Filtering.Net.Generator.Tests/` that asserts the diagnostic fires (and does not fire when the offending construct is removed).

## Snapshot-test workflow

`tests/Filtering.Net.Generator.Tests/Emission/Snapshots/` holds `.verified.cs` baselines under `Verify.Xunit`. When a generator change intentionally alters emitted output:

1. `dotnet test` — failing tests write `.received.cs` siblings.
2. Inspect each `.verified.cs` vs `.received.cs` diff and confirm semantic equivalence (`diff -uw` ignores whitespace; non-whitespace differences must be traceable to the template/emitter change you intended).
3. Bless. PowerShell:

   ```powershell
   Get-ChildItem -Recurse -Filter '*.received.cs' |
     ForEach-Object { Move-Item -Force $_ ($_ -replace '\.received\.cs$', '.verified.cs') }
   ```

   Bash:

   ```sh
   find . -name '*.received.cs' -exec sh -c 'mv "$1" "${1%.received.cs}.verified.cs"' _ {} \;
   ```

4. Re-run `dotnet test` to confirm green.

Compile-and-run and runtime-behaviour tests (`*_Compiles`, `EmittedCodeCompilesTests`, `EndToEndRuntimeTests`) MUST stay green at every step — only snapshot diffs may go red mid-refactor.

## See also

- [Filtering.Net on GitHub](https://github.com/sheva-serga/Filtering.NET)
