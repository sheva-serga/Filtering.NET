---
title: FAQ
description: Common gotchas and questions.
---

# FAQ

## Why is FN0014 firing on my string properties?

Declaring any custom `string` profile (e.g., a `[FilterProfile<string>(BasedOn = typeof(StringFilter))]`) makes profile resolution for `string` properties ambiguous: both the built-in `StringFilter` and your custom profile match. The generator refuses to guess. Fix it by specifying `Profile = typeof(...)` on every `string`-typed `[Map]`. The sample app's `UserFilter` does exactly this on `MapName`, `MapEmail`, and `MapDepartmentName`. See FN0014 in the [diagnostics catalogue](diagnostics/index.md) and [Built-in profiles](guides/built-in-profiles.md).

## How do I filter through a navigation property?

Use a dotted path on `[Map]` plus an `Alias` for the public-facing field name:

```csharp
[Map("Department.Name", Alias = "departmentName")]
public partial void MapDepartmentName(IPropertyMap<User, string> map);
```

Callers send `{ "field": "departmentName", ... }`. See [Navigation paths and aliases](guides/navigation-paths.md).

## Why does my custom operator fail at runtime even though FN1007 didn't fire?

`FN1007` is not exhaustive — it covers a known-good translatable allow-list only. EF Core's actual translatability is provider-specific (Postgres vs SQL Server vs SQLite all differ), and your custom method/extension may simply not be in the allow-list yet. Always test custom operators end-to-end against the target database before shipping.

## Can I use this with Dapper / Marten / EF6?

No. The library targets EF Core 8 / 9. The runtime types build expression trees the way the EF Core LINQ provider expects; non-EF-Core query providers (Dapper, Marten, EF6, raw `IEnumerable<T>` with custom translation) are out of scope.

## Does the source generator work in Native AOT?

Yes. The generated schema and the runtime engine use no reflection over your types and never call `Compile()`, so AOT-published apps are fine. Pair `AddFiltering` with a `JsonSerializerContext` (the `AddFiltering(IJsonTypeInfoResolver)` overload) to silence IL2026 / IL3050 warnings emitted by the typed-value deserialization path. See [Trim / AOT-clean setup](guides/aot-clean-setup.md).

## How do I add a custom operator that takes two values (e.g., "between")?

A built-in `between` operator already exists for numerics and temporals. For other multi-value custom operators you'll need to write a `[PropertyMap]` override that builds the expression manually — `[FilterOperator]` lambdas are single-value by design. See [Per-property override (PropertyMap)](guides/property-map-overrides.md).

## Can my interceptor method be `private`?

Yes. The generated schema code lives inside your filter class, so it can reference `private` interceptor and `[PropertyMap]` methods. They do have to be `static`.

## Are predicates built at compile time or at runtime?

At runtime, per request, from typed pieces that were compiled ahead of time. The generator emits accessor lambdas and profile references; the engine splices them together with an `ExpressionVisitor`. See [How it works](concepts/how-it-works.md).

## How do I disable a built-in operator on a specific property?

Use the `Except` argument on `[Map]`:

```csharp
[Map(nameof(User.Email), Except = new[] { "in" })]
public partial void MapEmail(IPropertyMap<User, string> map);
```

See [Restricting operators](guides/restricting-operators.md).

## Why don't my snapshot tests update?

Failing snapshot tests write `.received.cs` siblings next to the `.verified.cs` baseline; you must inspect each diff and rename `.received.cs` → `.verified.cs` to bless. The renaming is intentional friction so reviewers see baseline changes in the diff. See [Contributing](contributing.md) for the bless workflow.

## Can I share filter classes across multiple entities?

No. `[GenerateFilter<T>]` is per-entity. What you can share is profiles — declare a reusable `[FilterProfile<TColumn>]` static class once and reference it from any number of filter classes via `Profile = typeof(...)` on individual `[Map]` declarations.

## See also

- [Built-in profiles](guides/built-in-profiles.md)
- [Handling validation errors](guides/handling-validation-errors.md)
- [Diagnostics catalogue](diagnostics/index.md)
