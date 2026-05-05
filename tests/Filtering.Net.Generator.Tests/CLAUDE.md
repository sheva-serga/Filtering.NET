# CLAUDE.md — Filtering.Net.Generator.Tests

Three families of tests for the source generator, mixed in this one project because they all need the generator to actually run:

1. **Extraction / pipeline tests** — `ExtractionTests.cs`, `PipelineTests.cs`, `ModelExtraction/`, `Discovery/`. Asserts the generator produces the right model from a syntax tree, including diagnostics on bad input. Uses `GeneratorRunner.cs` to run the generator against an in-memory `Compilation`.
2. **Emission tests** — `Emission/`. Two flavours per scenario:
   - `*_Compiles` — runs the generator + compiles the emitted output, asserts no diagnostics. Catches regressions where the emitted code wouldn't actually build at consumer side. **These MUST stay green at every step of any refactor.**
   - `*_EmitsXxx` — `Verify.Xunit` snapshot of the full emitted file. Compared against `Snapshots/*.verified.cs`. Re-blessed wholesale when emitter changes are intentional.
3. **End-to-end runtime tests** — `Emission/EndToEndRuntimeTests.cs`. Compiles the generator output, loads it, runs the resulting `IFilterDefinition<T>` against in-memory data, asserts behaviour. Catches semantic regressions that compile-clean tests miss.

**Target:** `net9.0`. xUnit v3 + AwesomeAssertions + Verify.XunitV3 + Verify.SourceGenerators + Microsoft.CodeAnalysis.CSharp.

## Conventions

- **Framework:** xUnit v3. `[Fact]` for parameterless cases, `[Theory]` + `[InlineData]` for single-primitive parameterised cases, `[Theory]` + typed `TheoryData<…>` for multi-arg or non-primitive parameterised cases. No `[ClassData]`.
- **Assertions:** AwesomeAssertions `Should()` chains. The `because:` argument is required only when the assertion failure message wouldn't be obvious from the diff — never as filler.
- **AAA structure:** Every test method has explicit `// Arrange`, `// Act`, `// Assert` markers. The `// Arrange` marker may be omitted only when there is genuinely nothing to arrange.
- **Naming:** Test methods named `Method_Scenario_ExpectedResult` (e.g., `Apply_NullRequest_ThrowsArgumentNullException`). Test classes named `<Type>Tests.cs`, one class per source class.

## Snapshot bless workflow

Failing snapshot tests write `.received.cs` siblings next to each `.verified.cs`. When the generator change is intentional:

```powershell
Get-ChildItem -Path "tests/Filtering.Net.Generator.Tests/Emission/Snapshots" -Recurse -Filter '*.received.cs' `
  | ForEach-Object { Move-Item -Force $_.FullName ($_.FullName -replace '\.received\.cs$', '.verified.cs') }
```

Inspect the diff first — `diff -uw verified.cs received.cs` to ignore whitespace. Anything beyond whitespace + intentional stylistic drift (e.g., trailing comma in a switch expression) is a regression: revert and fix the emitter or template before blessing.

## Test runner + verifiers

- **`GeneratorRunner.cs`** — runs `FilterGenerator` against an in-memory `CSharpCompilation`, returns the resulting `GeneratorDriverRunResult`. The single entry point used by extraction, diagnostic, snapshot, and end-to-end tests.
- **`Emission/CompileVerifier.cs`** — drives the generator and compiles the result with the test project's references attached, returning compile diagnostics for assertion. Used by every `*_Compiles` test.
- **`Emission/ModuleInitializer.cs`** — Verify.Xunit global configuration (snapshot path, scrubbers) loaded once per test run.
- **`Emission/RuntimeLoader.cs`** — small reflection helper that loads the generator's emitted assembly into the test process. Used only by `EndToEndRuntimeTests`.

## Adding a snapshot test

1. New `*EmissionTests.cs` in `Emission/` with a `[Fact]` whose body calls `GeneratorRunner.Run(consumerSource)` and passes the resulting driver to `Verifier.Verify(driver)`.
2. First run produces `.received.cs` files and fails. Inspect them. If correct, rename each `.received.cs` to `.verified.cs` (or use the bless workflow above) and commit.
3. Pair with a `_Compiles` `[Fact]` that calls `CompileVerifier.AssertCompilesCleanly(consumerSource)` — your safety net during future refactors.

## Test framework caveats

- The test project takes a real `Microsoft.EntityFrameworkCore` reference (9.0) so the `EF.Functions.*` allow-list FN1007 tests can resolve symbols — the *generator* itself never references EF Core.

## Run subsets

```sh
dotnet test tests/Filtering.Net.Generator.Tests --filter "FullyQualifiedName~Compiles"          # safety net
dotnet test tests/Filtering.Net.Generator.Tests --filter "FullyQualifiedName~EndToEndRuntime"   # behavior
dotnet test tests/Filtering.Net.Generator.Tests --filter "FullyQualifiedName~ApplyFilter"       # one feature
dotnet test tests/Filtering.Net.Generator.Tests --filter "FullyQualifiedName!~Snapshot"         # everything but snapshots
```
