---
title: Building a definition by hand
description: Use the runtime engine directly, without the source generator.
---

# Building a definition by hand

## What this does

`FilterDefinition<TEntity>` is a normal class. The source generator builds its schema for you, but nothing stops you from building one yourself. The result implements `IFilterDefinition<TEntity>` and works with `Apply` and `ApplyPagedAsync` like any generated filter.

## When to use

- A filter whose shape is decided at runtime, for example per tenant.
- A test that needs a tiny definition without declaring a partial class.
- A project that cannot take an analyzer reference.

For everything else prefer `[GenerateFilter<TEntity>]`. You get the compile-time diagnostics and the DI registration for free.

## Minimal code

```csharp
var schema = new FilterSchemaBuilder<User>(new FilterSettings())
    .Add(FilterProperty.Map<User, string>("Name", user => user.Name, StringFilter.Profile)
        .Sortable()
        .Build())
    .Add(FilterProperty.MapNullable<User, int>("Score", user => user.Score, Int32Filter.Profile)
        .Only("eq", "gt", "isNull")
        .Build())
    .Build();

IFilterDefinition<User> userFilter = new FilterDefinition<User>(schema);
```

`Map` is for a property whose type is the profile's column type. `MapNullable` is for a nullable value-type property, such as `int?` against `Int32Filter`.

## Variations

- **Custom profile.** `StringFilter.Profile.Extend("StringFilterPlus", FilterOperator.Value<string, string>("fuzzy", (column, value) => column.Contains(value), StringFilter.TryGetValue))`. `Extend` throws when a declared operator re-uses an inherited name; `ExtendWithOverrides(name, operators)` replaces the inherited one in place instead, keeping its position in the operator order. That is what the generator emits for a `[FilterProfile<T>(BasedOn = ...)]` class. Both compare names case-insensitively, and both still throw when one call declares the same name twice.
- **Standalone profile.** `FilterProfile<TColumn>.Create(name, operators)`.
- **Typed operator values.** `FilterOperator.Value(name, predicate)` without a parser deserializes the value through System.Text.Json. Pass `JsonSerializerOptions` with a type-info resolver to `FilterSchemaBuilder`, otherwise `Build()` throws.
- **Nesting.** `departmentSchema.LiftInto<User>(user => user.Department, "Department")` returns the department properties re-rooted on `User`. Add them with `AddRange`. The prefix must be non-blank. `only` / `except` name the nested schema's own paths: `null` means "no restriction", an explicitly empty `only` allows no direct property, and a dotless entry the nested schema does not expose throws. Dotted entries are not checked, because they name paths a further nesting contributes.
- **Reporting the page size.** `definition.ResolvePageSize(request.PageSize)` returns the size `ApplySorting` will apply, after the schema's `DefaultPageSize` and `MaxPageSize`. Use it instead of echoing the requested value back to the caller.
- **Circular nesting.** `FilterSchemaBuilder.AddNested(nestingContext, nestingKey, maxDepth, nestedSchemaFactory, navigation, prefix)` follows a nesting at most `maxDepth` times along one path. Start from `FilterNestingContext.Root` and pass the context the factory receives down to the nested schema. An unbounded cycle throws `FilterConfigurationException` instead of recursing forever.
- **Custom accessor.** `FilterProperty.MapRule(field, rule)` takes a `FilterRule` built with `FilterRuleBuilder`.
- **Introspection.** `definition.Schema.Properties` lists every field, alias, operator set, and sortable flag. Use it to feed OpenAPI descriptions or a UI.

## Pitfalls

- A wire key used twice, across fields and aliases and ignoring case, throws `FilterConfigurationException` from `Build()`.
- `Only` and `Except` throw when they name an operator the profile does not declare; `Alias` throws on a blank alias, and a blank field name throws as soon as the builder is created.
- `FilterRuleBuilder.For(...)` may be called exactly once per rule; a second call throws rather than silently re-pointing the operators declared before it.
- Implementing `IFilterDefinition<TEntity>` yourself rather than deriving from `FilterDefinition<TEntity>` means implementing `ResolvePageSize(int?)` too — it was added to the interface in 0.2.0.
- None of the `FN` analyzer rules run on hand-built schemas. Mistakes surface when the schema is built, not when the project compiles.

## See also

- [How it works](../concepts/how-it-works.md)
- [Profiles and operators](../concepts/profiles-and-operators.md)
