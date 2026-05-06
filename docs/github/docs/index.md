---
title: Filtering.Net
description: Type-safe, source-generated filter / sort / page library for IQueryable<T> and EF Core.
---

# Filtering.Net

[![Filtering.Net](https://img.shields.io/nuget/v/Filtering.Net?label=Filtering.Net&logo=nuget)](https://www.nuget.org/packages/Filtering.Net)
[![Filtering.Net.Generator](https://img.shields.io/nuget/v/Filtering.Net.Generator?label=Filtering.Net.Generator&logo=nuget)](https://www.nuget.org/packages/Filtering.Net.Generator)
[![Filtering.Net.EntityFrameworkCore](https://img.shields.io/nuget/v/Filtering.Net.EntityFrameworkCore?label=Filtering.Net.EntityFrameworkCore&logo=nuget)](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore)

Type-safe, source-generated filter / sort / page library for `IQueryable<T>` and EF Core. Define your filterable surface with attributes; the source generator emits a strongly-typed `IFilterDefinition<T>` plus a JSON-friendly request model that translates straight to SQL.

## Why?

- **Structured JSON over string DSLs.** API consumers post a typed `FilterRequest` (groups + leaves) instead of an opaque DSL fragment. No parser, no escape rules, no surprises.
- **Source-generated.** No runtime expression construction, no reflection on hot paths, no surprise client-side evaluation. The generator emits one typed predicate method per `(property, operator)` pair.
- **Validation first.** Every request is validated against the generated definition before EF Core ever sees it. Errors come back as a structured list of `FilterValidationError`s with paths and codes.
- **EF Core aware.** A 24-rule analyzer catches translatable-method mistakes at compile time. The runtime ships an `ApplyPagedAsync` helper for one-call paging.

## Install

The first two packages are required (runtime + generator). The EF Core helpers are optional — install them when your call site is an EF Core controller or handler that wants a one-call paged async response.

```bash
dotnet add package Filtering.Net
dotnet add package Filtering.Net.Generator
dotnet add package Filtering.Net.EntityFrameworkCore
```

## 30-second quickstart

Declare your entity and a `[GenerateFilter<T>]` partial:

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

Wire up DI in `Program.cs`:

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

See **[Getting started](getting-started/installation.md)** for the guided walkthrough.

## Where to next?

Building your first filter? Start with **[Getting started](getting-started/installation.md)**.

Looking for a specific use case? See the **[Guides](guides/mapping-properties.md)**.

Hit a compile-time analyzer rule? See the **[Diagnostics catalogue](diagnostics/index.md)**.

## See also

- [Your first filter](getting-started/your-first-filter.md)
- [How it works](concepts/how-it-works.md)
- [The FilterRequest JSON shape](concepts/filter-request-shape.md)
