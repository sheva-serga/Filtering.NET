# Filtering.Net.Generator

Roslyn incremental source generator + 25-rule analyzer for [Filtering.Net](https://www.nuget.org/packages/Filtering.Net/). Emits typed `IFilterDefinition<T>` implementations and a DI extension at compile time. Catches translatable-method mistakes before EF Core sees them.

This package is **analyzer-only** — it has no runtime DLL. Install it alongside `Filtering.Net`.

## What it solves

Hand-rolling `IFilterDefinition<T>` per filter shape is repetitive and error-prone — and the moment you reach for runtime expression construction, EF Core's translatability rules become a runtime surprise. This generator turns a declarative `[GenerateFilter<T>]` partial into a fully-typed predicate set, and the bundled analyzer rejects shapes that would fail at runtime (e.g., a string operator wired to a numeric column, or a custom operator that calls a non-translatable method).

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
public partial class UserFilter
{
    [Map(nameof(User.Id),       Sortable = true)] private static partial void MapId();
    [Map(nameof(User.Name),     Sortable = true)] private static partial void MapName();
    [Map(nameof(User.Age),      Sortable = true)] private static partial void MapAge();
    [Map(nameof(User.IsActive))]                  private static partial void MapIsActive();
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

25 rules total: 17 errors (`FN0001`–`FN0017`) and 8 warnings (`FN1001`–`FN1008`). Per-rule explainers live at [`docs/diagnostics/`](https://github.com/sheva-serga/Filtering.Net/tree/master/docs/diagnostics) on GitHub.

| Id | Severity | Summary |
|----|----------|---------|
| FN0001 | Error | Property is mapped by multiple `[Map]` methods — each property must have at most one `[Map]` declaration. |
| FN0002 | Error | Property is marked `Sortable = true` on multiple `[Map]` methods. |
| FN0003 | Error | Property has both a `[Map]` and a `[PropertyMap]` — use one or the other. |
| FN0004 | Error | Property referenced by `[Map]` or `[PropertyMap]` does not exist on the entity type. |
| FN0005 | Error | Profile cannot be applied to the property — the profile's column type is incompatible with the property's CLR type. |
| FN0006 | Error | Operator referenced in `For(...).Operator(...)` is not declared by the resolved profile. |
| FN0007 | Error | Type referenced by `[ConvertWith]` does not inherit from `ValueConverter<TModel, TProvider>`. |
| FN0008 | Error | `[Map]` method is not declared `partial` — the generator can only emit implementations for partial methods. |
| FN0009 | Error | Property's CLR type has no built-in primitive profile; specify `Profile = typeof(...)` explicitly. |
| FN0010 | Error | Property has multiple `[InterceptValue]` declarations. |
| FN0011 | Error | `[FilterOperator]` member is not `public static`. |
| FN0012 | Error | Alias collides with another property or alias on the entity (case-insensitive). |
| FN0013 | Error | `[FilterProfile(BasedOn = typeof(...))]` references a type that is not marked with `[FilterProfile]`. |
| FN0014 | Error | Property has `[InterceptValue]` but no matching `[Map]` declaration. |
| FN0015 | Error | Property's CLR type is matched by multiple profiles — use `Profile = typeof(...)` on the `[Map]` to pick one. |
| FN0016 | Error | Standalone profile has no `BasedOn` and is missing required extractor method(s). |
| FN0017 | Error | Same operator name declared more than once on a single profile. |
| FN1001 | Warning | `[FilterOperator]` body references `DateTime.UtcNow`/`Now` directly inside the lambda. |
| FN1002 | Warning | Property is mapped but not marked `Sortable = true` — likely omission for a sortable type. |
| FN1003 | Warning | Profile is declared but never referenced by any `[Map(..., Profile = ...)]`. |
| FN1004 | Warning | Operator is declared on a profile but never referenced. |
| FN1005 | Warning | Property allows zero operators — `Only`/`Except` excluded everything; filter leaves on this field will always fail validation. |
| FN1006 | Warning | Mapped path crosses a nullable navigation property. |
| FN1007 | Warning | `[FilterOperator]` body calls a method not in the EF Core translatable allow-list — may produce client-side evaluation or runtime errors. |
| FN1008 | Warning | Path B filter value type is not registered in any visible `JsonSerializerContext` (opt-in via `[assembly: FilterValueDiagnostics(WarnUnregistered = true)]`). |

Click any rule on GitHub for the full explainer with examples + how to fix.

## Editor / build integration

The generator integrates with Roslyn directly — no MSBuild wiring required beyond installing the NuGet. Diagnostics surface in Visual Studio, Rider, VS Code (via OmniSharp / C# Dev Kit), and `dotnet build`.

## See also

- [Documentation site](https://sheva-serga.github.io/Filtering.NET/) — full guides, API reference, diagnostics catalogue.
- [Repo on GitHub](https://github.com/sheva-serga/Filtering.NET) — source, issue tracker, contribution notes.
- [`Filtering.Net`](https://www.nuget.org/packages/Filtering.Net/) — runtime types the emitted code references.
- [`Filtering.Net.EntityFrameworkCore`](https://www.nuget.org/packages/Filtering.Net.EntityFrameworkCore/) — async `ApplyPagedAsync` + `PageResult<T>`.

## License

MIT.
