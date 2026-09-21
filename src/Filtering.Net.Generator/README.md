# Filtering.Net.Generator

Roslyn incremental source generator + 37-rule analyzer for [Filtering.Net](https://www.nuget.org/packages/Filtering.Net/). Emits a typed filter schema per `[GenerateFilter<T>]` class, over the `FilterDefinition<T>` engine in `Filtering.Net`, plus a DI extension. Catches translatable-method mistakes before EF Core sees them.

This package is **analyzer-only** — it has no runtime DLL. Install it alongside `Filtering.Net`.

## What it solves

Hand-rolling `IFilterDefinition<T>` per filter shape is repetitive and error-prone — and the moment you reach for runtime expression construction, EF Core's translatability rules become a runtime surprise. This generator turns a declarative `[GenerateFilter<T>]` partial into a fully-typed filter schema, and the bundled analyzer rejects shapes that would fail at runtime (e.g., a string operator wired to a numeric column, or a custom operator that calls a non-translatable method).

## Install

```sh
dotnet add package Filtering.Net
dotnet add package Filtering.Net.Generator
```

Both are required: `Filtering.Net` ships the runtime types the generator's emitted code references; `Filtering.Net.Generator` ships the generator + analyzer.

## Quickstart

Declare a partial filter class:

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

The generator emits:
- The other half of `UserFilter`: a partial declaring `FilterDefinition<User>` as the base class, a constructor, and a `CreateSchema` method with one entry per `[Map]`. `Validate(...)`, `ApplyFilter(...)`, and `ApplySorting(...)` come from the base engine in `Filtering.Net`.
- A `services.AddFiltering()` extension method (when `Microsoft.Extensions.DependencyInjection.Abstractions` is referenced) that registers every generated filter class as a singleton `IFilterDefinition<T>`.

Wire it up:

```csharp
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddFiltering();   // <- emitted by the generator
```

## Custom operators

A *profile* is a static class decorated with `[FilterProfile<T>]` that defines a set of operators. Built-in profiles cover common types; declare your own to add custom logic. `BasedOn` inherits every operator of another profile, so you only declare the additions:

```csharp
[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class StringFilterPlus
{
    [FilterOperator("startsWithFold")]
    public static Expression<Func<string, string, bool>> StartsWithFold =>
        (column, value) => EF.Functions.ILike(column, value + "%");
}
```

Then `[Map(nameof(User.Name), Profile = typeof(StringFilterPlus))]` and the new operator is available on that property.

## Diagnostics

37 rules total: 29 errors (`FN0001`–`FN0029`) and 8 warnings (`FN1001`–`FN1008`). The full catalogue with one-line summaries lives at the [diagnostics catalogue](https://sheva-serga.github.io/Filtering.NET/diagnostics/) on the docs site.

| Id | Severity | Summary |
|----|----------|---------|
| FN0001 | Error | Filter path is mapped by multiple sources (`[Map]`, `[PropertyMap]`, or `[MapNested]`) on the same filter class. |
| FN0002 | Error | Property has both a `[Map]` and a `[PropertyMap]` — use one or the other. |
| FN0003 | Error | Property referenced by `[Map]` or `[PropertyMap]` does not exist on the entity type. |
| FN0004 | Error | Profile cannot be applied to the property — the profile's column type is incompatible with the property's CLR type. |
| FN0005 | Error | Operator named in `[Map(Only = ...)]` / `[Map(Except = ...)]` is not declared by the resolved profile. |
| FN0006 | Error | Property's CLR type has no built-in primitive profile; specify `Profile = typeof(...)` explicitly. |
| FN0007 | Error | Property has multiple `[InterceptValue]` declarations. |
| FN0008 | Error | `[FilterOperator]` member is not `public static`. |
| FN0009 | Error | Alias collides with another property or alias on the entity (case-insensitive). |
| FN0010 | Error | `[FilterProfile(BasedOn = typeof(...))]` references a type that is not marked with `[FilterProfile]`. |
| FN0011 | Error | Property has `[InterceptValue]` but no matching `[Map]` declaration. |
| FN0012 | Error | Property's CLR type is matched by multiple profiles — use `Profile = typeof(...)` on the `[Map]` to pick one. |
| FN0013 | Error | Same operator name declared more than once on a single profile (names are compared case-insensitively). |
| FN0014 | Error | `[MapNested]` graph contains a cycle in which no nesting declares `MaxDepth`. |
| FN0015 | Error | `[MapNested<T>]` does not name a `[GenerateFilter<TNavigation>]` partial in this compilation. |
| FN0016 | Error | Auto-resolve `[MapNested]` finds two or more `[GenerateFilter<TNav>]` candidates. |
| FN0017 | Error | Auto-resolve finds zero `[GenerateFilter<TNav>]` candidates for the navigation target type. |
| FN0018 | Error | Named property doesn't exist, isn't a reference type, or is a primitive/value type. |
| FN0019 | Error | Named navigation is a collection type; collection navigations are deferred to a future version. |
| FN0020 | Error | The `[GenerateFilter]` partial declares a base class; the generated part derives from `FilterDefinition<TEntity>`. |
| FN0021 | Error | `[MapNested]` declares a negative `MaxDepth` or one above 64 (`0`, the default, means unbounded). |
| FN0022 | Error | `[Map(Profile = typeof(X))]` references a type that is not marked with `[FilterProfile<TColumn>]`. |
| FN0023 | Error | `[PropertyMap]` method has a signature the generated `CreateSchema` cannot call. |
| FN0024 | Error | A dotted `[Map]` path reads a member through a `Nullable<T>` segment. |
| FN0025 | Error | `[MapNested]` declares a blank `Prefix`. |
| FN0026 | Error | A dotless `[MapNested]` `Only` / `Except` entry names a path the nested filter class does not map. |
| FN0027 | Error | The `[GenerateFilter]` partial is nested in another type or declares type parameters. |
| FN0028 | Error | A `[FilterOperator]` member is not an `Expression<Func<...>>` operator template. |
| FN0029 | Error | `[InterceptValue]` method has a signature the generated `CreateSchema` cannot call. |
| FN1001 | Warning | `[FilterOperator]` body references `DateTime.UtcNow`/`Now` directly inside the lambda. |
| FN1002 | Warning | Property is mapped but not marked `Sortable = true` — likely omission for a sortable type. |
| FN1003 | Warning | Profile is declared but never referenced by any `[Map(..., Profile = ...)]`. |
| FN1004 | Warning | Operator is declared on a profile but never referenced. |
| FN1005 | Warning | Property allows zero operators — `Only`/`Except` excluded everything; filter leaves on this field will always fail validation. |
| FN1006 | Warning | Mapped path crosses a nullable navigation property. |
| FN1007 | Warning | `[FilterOperator]` body calls a method not in the EF Core translatable allow-list — may produce client-side evaluation or runtime errors. |
| FN1008 | Warning | Filter value type is not registered in any visible `JsonSerializerContext` (opt-in via `[assembly: FilterValueDiagnostics(WarnUnregistered = true)]`). |

Every rule's `helpLinkUri` points back at the catalogue table on the docs site.

## Editor / build integration

The generator integrates with Roslyn directly — no MSBuild wiring required beyond installing the NuGet. Diagnostics surface in Visual Studio, Rider, VS Code (via OmniSharp / C# Dev Kit), and `dotnet build`.

## See also

- [Documentation site](https://sheva-serga.github.io/Filtering.NET/) — full guides, API reference, diagnostics catalogue.
- [Repo on GitHub](https://github.com/sheva-serga/Filtering.NET) — source, issue tracker, contribution notes.
- [`Filtering.Net`](https://www.nuget.org/packages/Filtering.Net/) — runtime types the emitted code references.
- [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) — async `ApplyPagedAsync` + `PageResult<T>`.

## License

MIT.
