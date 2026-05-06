---
title: Defining custom profiles + custom operators
description: Declare [FilterProfile<T>] and [FilterOperator] to add provider-specific or domain-specific operators.
---

# Defining custom profiles + custom operators

## What this does

A *custom profile* is a `static class` decorated with `[FilterProfile<TColumn>]`. It declares a set of operators that apply to columns of CLR type `TColumn`. Each operator is a `public static` property decorated with `[FilterOperator("name")]` that returns an `Expression<Func<TColumn, TValue, bool>>` (binary) or `Expression<Func<TColumn, bool>>` (unary). Setting `BasedOn = typeof(BuiltinProfile)` inherits every operator from the base profile so you only declare the additions.

## When to use

- Provider-specific operators — `EF.Functions.ILike` on PostgreSQL, full-text-search calls on SQL Server.
- Domain-specific operators — `withinDistance`, `fuzzy`, `containsTag`, anything your application's vocabulary needs.

## Minimal code

The sample app's `StringFilterPlus` inherits the built-in `StringFilter` and adds two operators:

```csharp
using System.Linq.Expressions;
using Filtering.Net;
using Microsoft.EntityFrameworkCore;

// Inherits every operator from StringFilter (eq, ne, contains, startsWith, endsWith, in, isNull) via BasedOn,
// then adds two more.
[FilterProfile<string>(BasedOn = typeof(StringFilter))]
public static class StringFilterPlus
{
    // Case-insensitive substring — String.Contains translates to a SQL LIKE on most providers.
    [FilterOperator("fuzzy")]
    public static Expression<Func<string, string, bool>> Fuzzy =>
        (column, value) => column.Contains(value.ToLower());

    // EF.Functions.* inside a [FilterOperator] body — translates to PostgreSQL ILIKE under Npgsql.
    [FilterOperator("ilike")]
    public static Expression<Func<string, string, bool>> ILike =>
        (column, pattern) => EF.Functions.ILike(column, pattern);
}
```

A `[Map(nameof(User.Name), Profile = typeof(StringFilterPlus))]` then makes `fuzzy` and `ilike` available on `User.Name`. The lambda body is inlined into the per-property `Build` method — there is no delegate dispatch at request time.

## Variations

- **Standalone profiles** — omit `BasedOn` and declare every operator from scratch. A standalone profile must also implement the JSON value extractors the generator calls (`TryGetValue`, `TryGetArray` for whichever value shapes the operators use), or `FN0015` fires.
- **Multiple profiles per CLR type** — declaring more than one profile for the same `TColumn` is allowed but makes built-in resolution ambiguous. Every `[Map]` for a property of that type must then specify `Profile = typeof(...)` explicitly (the sample app's `StringFilterPlus` triggers this contract for `string`).
- **Unary operators** — return `Expression<Func<TColumn, bool>>` for operators like `isNull` that take no value.

## Pitfalls

- `[FilterOperator]`-decorated members must be `public static`, otherwise `FN0010` fires.
- The same operator name cannot appear twice on one profile — `FN0016` flags duplicates (including duplicates that arise via `BasedOn` if you re-declare an inherited operator with a different body).
- Operator bodies that call methods outside EF Core's translatable allow-list emit `FN1007`. The operator still compiles and runs, but the predicate may fall back to client-side evaluation or fail at query time depending on the provider.

## See also

- [Intercepting filter values](intercepting-values.md)
