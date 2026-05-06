# Filtering.Net

[![NuGet — Filtering.Net](https://img.shields.io/nuget/v/Filtering.Net.svg?label=Filtering.Net)](https://www.nuget.org/packages/Filtering.Net/)
[![NuGet — Filtering.Net.Generator](https://img.shields.io/nuget/v/Filtering.Net.Generator.svg?label=Filtering.Net.Generator)](https://www.nuget.org/packages/Filtering.Net.Generator/)
[![NuGet — Filtering.Net.EntityFrameworkCore](https://img.shields.io/nuget/v/Filtering.Net.EntityFrameworkCore.svg?label=Filtering.Net.EntityFrameworkCore)](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Type-safe, source-generated filter / sort / page library for `IQueryable<T>` and EF Core. Define your filterable surface with attributes; the source generator emits a strongly-typed `IFilterDefinition<T>` plus a JSON-friendly request model that translates straight to SQL.

## Documentation

Full guides, API reference, and the diagnostics catalogue live at **<https://sheva-serga.github.io/Filtering.NET/>**.

## Why?

- **Structured JSON over string DSLs.** API consumers post a typed `FilterRequest` (groups + leaves) instead of an opaque DSL fragment. No parser, no escape rules, no surprises.
- **Source-generated.** No runtime expression construction, no reflection on hot paths, no surprise client-side evaluation. The generator emits one typed predicate method per `(property, operator)` pair.
- **Validation first.** Every request is validated against the generated definition before EF Core ever sees it. Errors come back as a structured list of `FilterValidationError`s with paths and codes.
- **EF Core aware.** A 24-rule analyzer catches translatable-method mistakes at compile time. The runtime ships an `ApplyPagedAsync` helper for one-call paging.

## Quick start

Declare your entity and a partial filter class:

```csharp
public sealed class User
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = "";
    public int    Age       { get; set; }
    public bool   IsActive  { get; set; }
}

[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Id),       Sortable = true)] private static partial void MapId();
    [Map(nameof(User.Name),     Sortable = true)] private static partial void MapName();
    [Map(nameof(User.Age),      Sortable = true)] private static partial void MapAge();
    [Map(nameof(User.IsActive))]                  private static partial void MapIsActive();
}
```

The source generator emits `UserFilter : IFilterDefinition<User>` with `Validate(...)`, `ApplyFilter(...)`, and `ApplySorting(...)` already implemented. Wire the generated DI extension up in `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddFiltering(); // emitted by the generator
```

Use it from a controller:

```csharp
[HttpPost("search")]
public async Task<ActionResult<PageResult<User>>> Search(
    [FromBody] FilterRequest request,
    [FromServices] IFilterDefinition<User> userFilter,
    [FromServices] AppDbContext dbContext,
    CancellationToken cancellationToken)
{
    try
    {
        var page = await dbContext.Users
            .ApplyPagedAsync(userFilter, request, cancellationToken);
        return Ok(page);
    }
    catch (FilterValidationException invalid)
    {
        return BadRequest(invalid.Result);
    }
}
```

A request body looks like:

```json
{
  "where": {
    "and": [
      { "field": "Name", "op": "contains", "value": "ali" },
      { "field": "IsActive", "op": "eq", "value": true }
    ]
  },
  "sort": [{ "field": "Age", "dir": 1 }],
  "page": 1,
  "pageSize": 25
}
```

## Package layout

| Package | Targets | What's in it |
|---------|---------|--------------|
| `Filtering.Net` | `netstandard2.0` | Runtime types: `FilterRequest`, `FilterNode`, `IFilterDefinition<T>`, `FilterValidationException`, `[GenerateFilter<T>]`, `[Map]`, `[FilterProfile]`, …, plus the `Apply` `IQueryable` extension. |
| `Filtering.Net.Generator` | `netstandard2.0` | Roslyn incremental source generator + 24-rule analyzer (`FN0001`–`FN0016` errors, `FN1001`–`FN1008` warnings). Templates are source-embedded Scriban; the analyzer DLL ships with no runtime NuGet dependencies. Consumed as an analyzer reference. |
| `Filtering.Net.EntityFrameworkCore` | `net8.0`, `net9.0`, `net10.0` | EF Core async helpers: `IQueryable<T>.ApplyPagedAsync(...)` and `PageResult<T>`. |

## Sample app

See [`samples/UserManagement.WebApi/`](samples/UserManagement.WebApi/README.md) for a full ASP.NET Core 9 + PostgreSQL walkthrough including a `docker-compose.yml`.

## Contributing

- `dotnet build` and `dotnet test` are the two pre-merge checks. Tests target xUnit v3 + AwesomeAssertions, follow the AAA convention with explicit `// Arrange` / `// Act` / `// Assert` markers, and name methods `Method_Scenario_ExpectedResult`.
- TreatWarningsAsErrors is on across the solution. The generator emits clean code against `<Nullable>enable</Nullable>` consumers.
- Snapshot tests use `Verify.XunitV3`; if you intentionally change the emitted shape, inspect the diff (`diff -uw verified.cs received.cs`), then accept (`mv *.received.cs *.verified.cs`) and commit alongside the source change.
- Generator emission is template-driven (Scriban). When changing what gets emitted, update the `.scriban` template under `src/Filtering.Net.Generator/Emission/Templates/` and the matching view-model record under `src/Filtering.Net.Generator/Emission/Views/`. Snapshot diffs may go red mid-refactor; compile-and-run tests (`*_Compiles`, `EmittedCodeCompilesTests`) MUST stay green at every step.
- New diagnostics get an entry in `DiagnosticDescriptors.cs` *and* a row in `docs/github/docs/diagnostics/index.md` (the mkdocs-material catalogue page).

## License

MIT.
