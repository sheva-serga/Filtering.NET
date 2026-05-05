# CLAUDE.md — Filtering.Net.EntityFrameworkCore

Thin EF Core async layer over `Filtering.Net`. Two public surfaces and nothing else.

**Targets:** `net8.0`, `net9.0`, and `net10.0` (multi-targeted to track LTS + current). Ships as `Filtering.Net.EntityFrameworkCore`.

## Public surface

- **`PageResult<T>`** (`PageResult.cs`) — record returned by `ApplyPagedAsync`. Carries `Items`, `TotalCount`, `Page`, `PageSize`. `TotalPages`, `HasPrevious`, and `HasNext` are derived; don't store them.
- **`FilteringEntityFrameworkExtensions`** — adds `ApplyPagedAsync(IFilterDefinition<T>, FilterRequest, CancellationToken)` to `IQueryable<T>`. Validates the request, applies filter + sort, runs `CountAsync` + `ToListAsync` against the EF provider, packages into `PageResult<T>`. Throws `FilterValidationException` when validation fails — callers convert to HTTP 400 (or domain equivalent).

## EF version pinning

```xml
<!-- 8.0.13 / 9.0.5 patch GHSA-qj66-m88j-hmgj on transitive Microsoft.Extensions.Caching.Memory. -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.13" Condition="'$(TargetFramework)' == 'net8.0'" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.5" Condition="'$(TargetFramework)' == 'net9.0'" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" Condition="'$(TargetFramework)' == 'net10.0'" />
```

Floor versions match the matching .NET TFM. Bumping these requires re-running the EF integration suite under both providers in `tests/Filtering.Net.EntityFrameworkCore.Tests/`.

## Why this is its own assembly

`Filtering.Net` itself is `netstandard2.0` and avoids EF Core entirely so it can load inside the analyzer process and on every consumer TFM. EF-specific async sequencing lives here. Anything that doesn't strictly require EF (`CountAsync`, `ToListAsync`) belongs in `Filtering.Net` — keep this assembly tiny.

## Editing rules

- **Don't reach into the generator's internals.** This assembly only consumes `IFilterDefinition<T>` + `FilterRequest`, never the generator output shape. If something here needs to know about emitted code, it belongs in the generator instead.
- **Keep the public surface small.** Two types currently. Adding a new public method requires a corresponding test in `tests/Filtering.Net.EntityFrameworkCore.Tests/Scenarios/`.

## Tests

`tests/Filtering.Net.EntityFrameworkCore.Tests/` runs the same `Apply*` pipeline against:
- SQLite (in-memory, always available)
- Postgres (via Testcontainers; opt-in based on Docker availability)
- SQL Server (via Testcontainers; opt-in)

Scenarios in `Scenarios/` exercise translatability across providers — adding an operator to a built-in profile usually means adding a scenario here too.
