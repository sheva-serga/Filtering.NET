# Filtering.Net.Generator

Roslyn incremental source generator + 31-rule analyzer for [Filtering.Net](https://www.nuget.org/packages/Filtering.Net/). Emits a typed filter schema per `[GenerateFilter<T>]` class, over the `FilterDefinition<T>` engine in `Filtering.Net`, plus a DI extension. Catches translatable-method mistakes before EF Core sees them.

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
public partial class UserFilter
{
}
```

The generator emits:
- `UserFilter : IFilterDefinition<User>` with `Validate(...)`, `ApplyFilter(...)`, `ApplySorting(...)` already implemented.
- A `services.AddFiltering()` extension method (when `Microsoft.Extensions.DependencyInjection.Abstractions` is referenced) that registers every generated filter class as `IFilterDefinition<T>`.

Wire it up:

```csharp
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddFiltering();   // <- emitted by the generator
```

## Custom operators

A *profile* is a static class decorated with `[FilterProfile<T>]` that defines a set of operators. Built-in profiles cover common types; declare your own to add custom logic:

```csharp
[FilterProfile<string>]
public static class StringFilterPlus
{
    [BasedOn(typeof(StringFilter))]
    private static void Inherit() { }

    [FilterOperator("startsWithFold")]
    public static Expression<Func<string, string, bool>> StartsWithFold() =>
        (column, value) => EF.Functions.ILike(column, value + "%");
}
```

Then `[Map(nameof(User.Name), Profile = typeof(StringFilterPlus))]` and the new operator is available on that property.

## Diagnostics

29 rules total: 21 errors (`FN0001`–`FN0021`) and 8 warnings (`FN1001`–`FN1008`). The full catalogue with one-line summaries lives at the [diagnostics catalogue](https://sheva-serga.github.io/Filtering.NET/diagnostics/) on the docs site.

| Id | Severity | Summary |
|----|----------|---------|
| FN0001 | Error | Filter path is mapped by multiple sources (`[Map]`, `[PropertyMap]`, or `[MapNested]`) on the same filter class. |
| FN0002 | Error | Property has both a `[Map]` and a `[PropertyMap]` — use one or the other. |
| FN0003 | Error | Property referenced by `[Map]` or `[PropertyMap]` does not exist on the entity type. |
| FN0004 | Error | Profile cannot be applied to the property — the profile's column type is incompatible with the property's CLR type. |
| FN0005 | Error | Operator referenced in `For(...).Operator(...)` is not declared by the resolved profile. |
| FN0006 | Error | `[Map]` method is not declared `partial` — the generator can only emit implementations for partial methods. |
| FN0007 | Error | Property's CLR type has no built-in primitive profile; specify `Profile = typeof(...)` explicitly. |
| FN0008 | Error | Property has multiple `[InterceptValue]` declarations. |
| FN0009 | Error | `[FilterOperator]` member is not `public static`. |
| FN0010 | Error | Alias collides with another property or alias on the entity (case-insensitive). |
| FN0011 | Error | `[FilterProfile(BasedOn = typeof(...))]` references a type that is not marked with `[FilterProfile]`. |
| FN0012 | Error | Property has `[InterceptValue]` but no matching `[Map]` declaration. |
| FN0013 | Error | Property's CLR type is matched by multiple profiles — use `Profile = typeof(...)` on the `[Map]` to pick one. |
| FN0014 | Error | Standalone profile has no `BasedOn` and is missing required extractor method(s). |
| FN0015 | Error | Same operator name declared more than once on a single profile. |
| FN0016 | Error | `[MapNested]` introduces a cycle in the filter-inlining graph. |
| FN0017 | Error | `[MapNested<T>]` references a filter class declared outside the current compilation. |
| FN0018 | Error | Auto-resolve `[MapNested]` finds two or more `[GenerateFilter<TNav>]` candidates. |
| FN0019 | Error | Auto-resolve finds zero `[GenerateFilter<TNav>]` candidates for the navigation target type. |
| FN0020 | Error | Named property doesn't exist, isn't a reference type, or is a primitive/value type. |
| FN0021 | Error | Named navigation is a collection type; collection navigations are deferred to a future version. |
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
