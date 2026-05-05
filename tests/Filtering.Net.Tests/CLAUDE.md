# CLAUDE.md — Filtering.Net.Tests

Runtime unit tests for the `Filtering.Net` package. **No source generator** runs here — these tests exercise the runtime types in isolation.

**Target:** `net9.0`. xUnit v3 + AwesomeAssertions.

## Folder structure mirrors the runtime

| Folder | Tests for |
|--------|-----------|
| `Attributes/` | Attribute construction + property defaults. |
| `Requests/` | JSON round-tripping for `FilterRequest`, `FilterNode` polymorphic discriminator, `SortItem`. |
| `Validation/` | `FilterValidationResult.Combine`, `FilterValidationError` shape, helper methods on `LeafValidation` / `PageValidation`. |
| `Composition/` | `PredicateBuilder.AndAlso/OrElse/Not` parameter rebinding correctness. |
| `Profiles/` | Built-in profile `TryGet*` extractor behaviour for valid + invalid JSON. |
| `Exceptions/` | `FilterValidationException.Result` / `Errors` exposure. |
| `FilteringQueryableExtensionsTests.cs` | `IQueryable<T>.Apply` orchestration: validation failure throws, success returns the filtered/sorted/paged query. |

## Conventions

- **Framework:** xUnit v3. `[Fact]` for parameterless cases, `[Theory]` + `[InlineData]` for single-primitive parameterised cases, `[Theory]` + typed `TheoryData<…>` for multi-arg or non-primitive parameterised cases. No `[ClassData]`.
- **Assertions:** AwesomeAssertions `Should()` chains. The `because:` argument is required only when the assertion failure message wouldn't be obvious from the diff — never as filler.
- **AAA structure:** Every test method has explicit `// Arrange`, `// Act`, `// Assert` markers. The `// Arrange` marker may be omitted only when there is genuinely nothing to arrange.
- **Naming:** Test methods named `Method_Scenario_ExpectedResult` (e.g., `Apply_NullRequest_ThrowsArgumentNullException`). Test classes named `<Type>Tests.cs`, one class per source class.

## What does NOT belong here

- **Anything that spins up the source generator** — that lives in `tests/Filtering.Net.Generator.Tests/`.
- **Anything that takes an EF Core dependency** — that lives in `tests/Filtering.Net.EntityFrameworkCore.Tests/`.
- **End-to-end "user declares a filter, request comes in, results come out" tests** — those belong in `Filtering.Net.Generator.Tests/Emission/EndToEndRuntimeTests.cs` because they need the generator to actually run.

## Run subset

```sh
dotnet test tests/Filtering.Net.Tests
dotnet test tests/Filtering.Net.Tests --filter "FullyQualifiedName~Profile"
```
