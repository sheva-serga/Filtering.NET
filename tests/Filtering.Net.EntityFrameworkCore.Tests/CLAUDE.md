# CLAUDE.md — Filtering.Net.EntityFrameworkCore.Tests

Integration tests for the EF async layer. Runs real EF Core queries against in-memory SQLite (always) and containerised Postgres / SQL Server (opt-in based on Docker availability).

**Target:** `net9.0`. xUnit v3 + AwesomeAssertions + Microsoft.EntityFrameworkCore + provider-specific packages.

## Conventions

- **Framework:** xUnit v3. `[Fact]` for parameterless cases, `[Theory]` + `[InlineData]` for single-primitive parameterised cases, `[Theory]` + typed `TheoryData<…>` for multi-arg or non-primitive parameterised cases. No `[ClassData]`.
- **Assertions:** AwesomeAssertions `Should()` chains. The `because:` argument is required only when the assertion failure message wouldn't be obvious from the diff — never as filler.
- **AAA structure:** Every test method has explicit `// Arrange`, `// Act`, `// Assert` markers. The `// Arrange` marker may be omitted only when there is genuinely nothing to arrange.
- **Naming:** Test methods named `Method_Scenario_ExpectedResult` (e.g., `Apply_NullRequest_ThrowsArgumentNullException`). Test classes named `<Type>Tests.cs`, one class per source class.

## Layout

| Folder | Contents |
|--------|----------|
| `Fixtures/` | xUnit class fixtures: `SqliteFixture`, `PostgresFixture`, `SqlServerFixture`, plus `ScenarioDbContext`, `ScenarioEntities`, `WidgetFilter`, `WidgetSeed`. `DockerAvailability.cs` short-circuits the Postgres/SQL Server fixtures when Docker isn't on the path. |
| `Scenarios/` | Per-feature integration scenarios. Each runs against multiple providers via fixture inheritance. `FilterRequestBuilder.cs` is a fluent helper for constructing `FilterRequest` shapes inline. |
| `ApplyPagedAsyncTests.cs` | Direct tests of the `ApplyPagedAsync` extension: validation failure → `FilterValidationException`; success → `PageResult<T>` with correct `TotalCount`. |
| `PageResultTests.cs` | `PageResult<T>` record + derived properties (`TotalPages`, `HasPrevious`, `HasNext`). |

## Provider matrix

- **SQLite** — `Microsoft.EntityFrameworkCore.Sqlite`, in-memory connection. Always runs.
- **Postgres** — `Npgsql.EntityFrameworkCore.PostgreSQL` via Testcontainers. Skipped (with `Skip` reason) when `DockerAvailability.IsAvailable` is false.
- **SQL Server** — `Microsoft.EntityFrameworkCore.SqlServer` via Testcontainers. Same Docker-availability skip.

`PostgresScenarios.cs` and `SqlParameterizationTests.cs` are the provider-specific edges — most scenarios run against all providers via shared base classes.

## Scenario conventions

- One scenario class per filter shape (`StringFilterScenarios`, `NumberFilterScenarios`, …).
- Each scenario seeds via `WidgetSeed.SeedAsync(context)` and asserts on the `Items` returned from `ApplyPagedAsync`.
- Postgres-specific scenarios live in `PostgresScenarios.cs` and use `Assert.Skip(reason)` to bail when Docker isn't running.

## Run subset

```sh
dotnet test tests/Filtering.Net.EntityFrameworkCore.Tests
dotnet test tests/Filtering.Net.EntityFrameworkCore.Tests --filter "FullyQualifiedName~Sqlite"
dotnet test tests/Filtering.Net.EntityFrameworkCore.Tests --filter "FullyQualifiedName~Postgres"
```

## When Docker is unavailable

The non-SQLite tests use `Assert.Skip("Docker is not available on this host.")` so the suite stays green on machines without Docker. CI runs with Docker enabled and exercises the full matrix.
