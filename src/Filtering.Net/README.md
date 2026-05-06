# Filtering.Net

Type-safe filter / sort / page request types for `IQueryable<T>`. Pair with [`Filtering.Net.Generator`](https://www.nuget.org/packages/Filtering.Net.Generator/) to get strongly-typed `IFilterDefinition<T>` implementations generated at compile time, and (optionally) [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) for `async` EF Core helpers.

## What it solves

API consumers post structured JSON — groups of leaves with operators and values — instead of an opaque DSL fragment. Every request is validated before EF Core ever sees it; errors come back as a typed list of `FilterValidationError`s with paths and codes. There is no runtime expression-tree construction and no reflection on hot paths: the source generator (separate package) emits one typed predicate per `(property, operator)` pair.

## Install

```sh
dotnet add package Filtering.Net
dotnet add package Filtering.Net.Generator   # the source generator (compile-time only)
```

If you only install `Filtering.Net`, you get the request types and the `IQueryable.Apply` extension, but you'll need to write `IFilterDefinition<T>` implementations by hand. Add the generator package to skip that work.

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
public partial class UserFilter
{
    [Map(nameof(User.Id),       Sortable = true)] private static partial void MapId();
    [Map(nameof(User.Name),     Sortable = true)] private static partial void MapName();
    [Map(nameof(User.Age),      Sortable = true)] private static partial void MapAge();
    [Map(nameof(User.IsActive))]                  private static partial void MapIsActive();
}
```

Apply a request to an `IQueryable<User>`:

```csharp
var request = new FilterRequest
{
    Where = new FilterGroup(LogicalOp.And,
    [
        new FilterLeaf("Name",     "contains", "ali"),
        new FilterLeaf("IsActive", "eq",       true),
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
- **`FilterNode`** — base; `FilterGroup` (`and`/`or` of children) and `FilterLeaf` (`field` + `operator` + `value`).
- **`SortItem`** — `field` + `dir` (`Asc` / `Desc`).
- **`IFilterDefinition<T>`** — composite interface every generated filter class implements: `Validate(...)`, `ApplyFilter(...)`, `ApplySorting(...)`.
- **`FilterValidationResult` / `FilterValidationError`** — structured error shape with JSON-pointer-style paths and codes.
- **`FilterValidationException`** — thrown by `Apply` when validation fails; carries the `Result` for HTTP 400 conversion.
- **Built-in profiles** — `StringFilter`, `BoolFilter`, `GuidFilter`, `DateTimeFilter`, plus `Numeric/*` and `Temporal/*` per primitive. The generator picks one automatically based on the property's CLR type; override with `[Map(..., Profile = typeof(MyProfile))]`.
- **Attributes** — `[GenerateFilter<T>]`, `[Map]`, `[PropertyMap]`, `[FilterProfile<T>]`, `[FilterOperator]`, `[FilterValidator]`, `[InterceptValue]`, `[FilterDefaults]`, `[PageSettings]`.

## Synchronous vs async

`IQueryable<T>.Apply(...)` is synchronous and returns `IQueryable<T>` — your call site decides whether to enumerate eagerly (`ToList`), lazily, or via `async` EF helpers. For a one-call paged async flow against EF Core, install [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) and use `ApplyPagedAsync(...)`.

## See also

- [Documentation site](https://sheva-serga.github.io/Filtering.NET/) — full guides, API reference, diagnostics catalogue.
- [Repo on GitHub](https://github.com/sheva-serga/Filtering.NET) — source, issue tracker, contribution notes.
- [`Filtering.Net.Generator`](https://www.nuget.org/packages/Filtering.Net.Generator/) — source generator + 24-rule analyzer that emits the `IFilterDefinition<T>` glue.
- [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) — async `ApplyPagedAsync` + `PageResult<T>`.
- Sample ASP.NET Core 9 + PostgreSQL app: [`samples/UserManagement.WebApi/`](https://github.com/sheva-serga/Filtering.NET/tree/main/samples/UserManagement.WebApi) on GitHub.

## License

MIT.
