# Filtering.Net

Type-safe filter / sort / page request types for `IQueryable<T>`. Pair with [`Filtering.Net.Generator`](https://www.nuget.org/packages/Filtering.Net.Generator/) to get a strongly-typed filter schema generated for each of your filter classes, and (optionally) [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) for `async` EF Core helpers.

## What it solves

API consumers post structured JSON — groups of leaves with operators and values — instead of an opaque DSL fragment. Every request is validated before EF Core ever sees it; errors come back as a typed list of `FilterValidationError`s with paths and codes. The filter engine in this package composes predicates from compiler-checked accessor and operator lambdas, with no reflection over your types and no `Compile()`. The source generator (separate package) emits the typed schema that feeds it, or you can build one by hand.

## Install

```sh
dotnet add package Filtering.Net
dotnet add package Filtering.Net.Generator   # the source generator (compile-time only)
```

If you only install `Filtering.Net`, you get the request types and the `IQueryable.Apply` extension, but you'll need to write `IFilterDefinition<T>` implementations by hand. Add the generator package to skip that work.

The package targets `netstandard2.0` and `net8.0`. `DateOnlyFilter` and `TimeOnlyFilter` ship in the `net8.0` asset only, because `DateOnly` / `TimeOnly` do not exist in `netstandard2.0`.

## Quickstart

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
[Map(nameof(User.Id),       Sortable = true)]
[Map(nameof(User.Name),     Sortable = true)]
[Map(nameof(User.Age),      Sortable = true)]
[Map(nameof(User.IsActive))]
public partial class UserFilter { }
```

Apply a request to an `IQueryable<User>`:

```csharp
using System.Text.Json;

// A leaf's value is the raw JSON value, so it is a JsonElement. Requests that arrive over HTTP
// are deserialized into this shape by FilterNodeJsonConverter; built by hand it looks like this.
var request = new FilterRequest
{
    Where = new FilterGroup(LogicalOp.And,
    [
        new FilterLeaf("Name",     "contains", JsonDocument.Parse("\"ali\"").RootElement),
        new FilterLeaf("IsActive", "eq",       JsonDocument.Parse("true").RootElement),
    ]),
    Sort     = [new SortItem("Age", SortDir.Asc)],
    Page     = 1,
    PageSize = 25,
};

IQueryable<User> result = users.Apply(userFilter, request);
```

`Apply(...)` validates first; on failure it throws `FilterValidationException` whose `.Result` carries the `FilterValidationResult` you'd return as HTTP 400.

## Key types

- **`FilterRequest`** — `where` (`FilterNode`), `sort` (`SortItem[]`), `page`, `pageSize`. Polymorphic JSON via `FilterNodeJsonConverter`.
- **`FilterNode`** — base; `FilterGroup` (`and` / `or` / `not` of children) and `FilterLeaf` (`field` + `op` + `value`, the value a `JsonElement`).
- **`SortItem`** — `field` + `dir` (`Asc` / `Desc`, nullable: an omitted `dir` uses the property's default direction).
- **`IFilterDefinition<T>`** — composite interface every generated filter class implements: `Validate(...)`, `ResolvePageSize(...)`, `ApplyFilter(...)`, `ApplySorting(...)`.
- **`FilterValidationResult` / `FilterValidationError`** — structured error shape with dotted request paths (`where.and[0].op`, `sort[1].field`, `pageSize`) and codes.
- **`FilterValidationException`** — thrown by `Apply` when validation fails; carries the `Result` for HTTP 400 conversion.
- **Built-in profiles** — `StringFilter`, `BoolFilter`, `GuidFilter`, `DateTimeFilter`, plus `Numeric/*` and `Temporal/*` per primitive. The generator picks one automatically based on the property's CLR type; override with `[Map(..., Profile = typeof(MyProfile))]`.
- **Attributes** — `[GenerateFilter<T>]`, `[Map]`, `[PropertyMap]`, `[FilterProfile<T>]`, `[FilterOperator]`, `[InterceptValue]`, `[FilterDefaults]`, `[PageSettings]`.

## Synchronous vs async

`IQueryable<T>.Apply(...)` is synchronous and returns `IQueryable<T>` — your call site decides whether to enumerate eagerly (`ToList`), lazily, or via `async` EF helpers. For a one-call paged async flow against EF Core, install [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) and use `ApplyPagedAsync(...)`.

## See also

- [Documentation site](https://sheva-serga.github.io/Filtering.NET/) — full guides, API reference, diagnostics catalogue.
- [Repo on GitHub](https://github.com/sheva-serga/Filtering.NET) — source, issue tracker, contribution notes.
- [`Filtering.Net.Generator`](https://www.nuget.org/packages/Filtering.Net.Generator/) — source generator + 37-rule analyzer that emits the typed filter schema.
- [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) — async `ApplyPagedAsync` + `PageResult<T>`.
- Sample ASP.NET Core 9 + PostgreSQL app: [`samples/UserManagement.WebApi/`](https://github.com/sheva-serga/Filtering.NET/tree/main/samples/UserManagement.WebApi) on GitHub.

## License

MIT.
