# Contributing to Filtering.Net

Thanks for helping. Bug reports, documentation fixes and pull requests are all welcome. For anything bigger than a fix, please open an issue first so we can agree on the shape before you write the code.

## Prerequisites

- **.NET SDKs 8, 9 and 10.** The libraries target `netstandard2.0`, `net8.0`, `net9.0` and `net10.0`; CI installs all three SDKs.
- **Docker** (optional). The PostgreSQL and SQL Server suites in `tests/Filtering.Net.EntityFrameworkCore.Tests` run on Testcontainers and are skipped when no Docker daemon is available.
- **Node.js 22 or later** for the TypeScript client in `clients/typescript`.
- **Python with `mkdocs-material` and `pymdown-extensions`** only if you work on the documentation site.

## Repository layout

| Path | What lives there |
|------|------------------|
| `src/Filtering.Net/` | Runtime: attributes, request types, the `FilterDefinition<TEntity>` engine, built-in profiles. |
| `src/Filtering.Net.Generator/` | Incremental source generator and analyzer. Emission is template-driven (Scriban). |
| `src/Filtering.Net.EntityFrameworkCore/` | EF Core helpers (`ApplyPagedAsync`, `PageResult<T>`). |
| `clients/typescript/` | The `filtering-net` npm package. |
| `tests/` | Runtime, generator and EF Core test projects, plus the shared `wire-fixtures/`. |
| `docs/github/` | The mkdocs-material documentation site. |
| `samples/UserManagement.WebApi/` | End-to-end ASP.NET Core sample. |

## Build and test

```sh
dotnet build              # whole solution
dotnet test               # all three test projects
dotnet test tests/Filtering.Net.Generator.Tests --filter "FullyQualifiedName~CompositeValidate"
```

```sh
cd clients/typescript
npm ci
npm run typecheck
npm test
npm run build
```

```sh
pip install mkdocs-material pymdown-extensions
mkdocs serve --config-file docs/github/mkdocs.yml          # live preview
mkdocs build --strict --config-file docs/github/mkdocs.yml # what CI runs on a release tag
```

`TreatWarningsAsErrors` is on across the solution, and the generated code must compile cleanly in consumers with `<Nullable>enable</Nullable>`. A build failure is often a warning promoted to an error, so read the actual message before suppressing anything.

## Coding conventions

- **Names spell things out.** `validationErrors`, not `errs`; `requestedPageSize`, not `rps`. Matching this style is a hard requirement.
- **Custom exception types only.** Throw `FilterValidationException`, `FilterDispatchException`, `FilterConfigurationException` or `FilterEmissionException`, never `InvalidOperationException` or `UnreachableException` from production code.
- **Composite interfaces over capability splits.** `IFilterDefinition<T>` carries every `Validate` / `ApplyFilter` / `ApplySorting` overload; it is not split into `IValidator`, `IPredicateBuilder` and so on.
- **Drop features rather than add magic defaults.** When a configuration is ambiguous, report a diagnostic instead of guessing.
- **One method per concern in the public DSL.** For example, `For(...)` and `.Operator(...)` are separate steps.
- **Malformed client input is a 400, never a 500.** Anything that parses or binds request data must fail with a validation error or a failed parse, not an exception that escapes as a server error.

## Tests

- xUnit v3 with AwesomeAssertions. Tests follow Arrange / Act / Assert with explicit `// Arrange`, `// Act`, `// Assert` markers and are named `Method_Scenario_ExpectedResult`.
- Every bug fix starts with a test that fails without the fix.
- Reuse the shared infrastructure before writing helpers: `GeneratedFilterHarness` and `RuntimeLoader` for generated-filter runtime tests, `DiagnosticTestHelpers` for analyzer tests, `EchoServerFixture` for ASP.NET binding tests, and the EF Core fixtures under `tests/Filtering.Net.EntityFrameworkCore.Tests/Fixtures`.

### Snapshot tests

`tests/Filtering.Net.Generator.Tests/Emission/Snapshots/` holds `.verified.cs` baselines under Verify. When a generator change intentionally alters the emitted code:

1. Run `dotnet test`. Failing snapshot tests write `.received.cs` files next to the baselines.
2. Compare each `.verified.cs` with its `.received.cs` (`diff -uw` ignores whitespace). Every non-whitespace difference must trace back to the change you intended.
3. Accept the new output. PowerShell:

   ```powershell
   Get-ChildItem -Recurse -Filter '*.received.cs' |
     ForEach-Object { Move-Item -Force $_ ($_ -replace '\.received\.cs$', '.verified.cs') }
   ```

   Bash:

   ```sh
   find . -name '*.received.cs' -exec sh -c 'mv "$1" "${1%.received.cs}.verified.cs"' _ {} \;
   ```

4. Run `dotnet test` again and commit the baselines together with the source change.

Snapshot diffs may go red in the middle of a refactor, but the compile-and-run tests (`*_Compiles`, `EmittedCodeCompilesTests`, `EndToEndRuntimeTests`) must stay green at every step. When you change what gets emitted, update both the `.scriban` template under `src/Filtering.Net.Generator/Emission/Templates/` and the matching view-model record under `src/Filtering.Net.Generator/Emission/Views/`.

### Adding a diagnostic

1. Add a `DiagnosticDescriptor` to `src/Filtering.Net.Generator/Diagnostics/DiagnosticDescriptors.cs` with the next `FN0xxx` (error) or `FN1xxx` (warning) id.
2. Add a row to `docs/github/docs/diagnostics/index.md`.
3. Wire it into the relevant extractor or analyzer.
4. Add a `tests/Filtering.Net.Generator.Tests/Diagnostics/Fn0xxxTests.cs` that shows the diagnostic firing, and not firing once the offending construct is removed. One mistake should produce exactly one diagnostic id.

### Changing the wire format

The .NET packages and the TypeScript client must agree byte for byte. Change the shared fixtures first:

- `tests/wire-fixtures/*.json` hold `name`, `request` and `queryString`. The Vitest suite and `tests/Filtering.Net.Tests/WireFixtures` both read them, including real ASP.NET query binding.
- `tests/wire-fixtures/invalid/query-strings.json` lists query strings the server must reject with a 400. Rows marked `clientAccepts: true` are shapes the TypeScript client deliberately passes through, because it validates nothing beyond the wire format.

## Pull requests

- Keep each commit to one change, with a short imperative subject such as `Report a null sort item as a validation error instead of throwing`.
- Add an entry to `CHANGELOG.md` under an `Unreleased` heading, following [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
- CI (`.github/workflows/ci.yml`) must pass: the .NET tests (including the Testcontainers suites) and the TypeScript typecheck, tests, build and package lint (`publint`, `arethetypeswrong`).

## Releases (maintainers)

The NuGet packages and the npm client are released together, always under the same version.

1. Rename the `Unreleased` heading in `CHANGELOG.md` to the version and date, commit `Date the X.Y.Z release`, push `main` and wait for CI.
2. Tag and push: `git tag vX.Y.Z && git push origin vX.Y.Z`. `.github/workflows/release.yml` runs both test suites, packs everything, pushes the NuGet packages and stages the npm package through npm Trusted Publishing, with provenance and no token.
3. Approve the staged npm version, which needs 2FA:

   ```sh
   npm login
   npm stage list filtering-net
   npm stage approve <stage-id>
   ```

If a publish job fails, fix the cause and use "Re-run failed jobs". It reuses the packages from the `pack` job, so nothing is rebuilt.
