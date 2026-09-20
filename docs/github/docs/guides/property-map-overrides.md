---
title: Per-property override with [PropertyMap]
description: Give one property a custom accessor and its own operators with a fluent For/Operator rule.
---

# Per-property override with `[PropertyMap]`

## What this does

`[PropertyMap]` is a per-property escape hatch that bypasses the profile system. Instead of picking operators from a profile, you return a rule: `builder.For(x => x.Path).Operator<TArg>("name", (column, value) => ...)`. The generated filter calls your method once, when the filter is constructed, and registers the accessor and operators you declared.

## When to use

Anything the profile system cannot express cleanly:

- Collection columns where you want `containsAny` or `containsAll` semantics over `List<T>` or `T[]`.
- JSONB columns where the predicate calls EF Core's JSON helpers.
- Computed accessors such as `u => u.FirstName + " " + u.LastName`.
- Full-text wrappers around provider-specific functions.

To add one operator to a CLR type's normal surface, prefer a `[FilterProfile<T>]` with `BasedOn`. It is reusable across properties.

## Minimal code

```csharp
[GenerateFilter<Article>]
public partial class ArticleFilter
{
    [Map(nameof(Article.Id), Sortable = true)]
    private static partial void MapId();

    [PropertyMap(nameof(Article.Tags))]
    private static FilterRule<Article, List<string>> MapTags(
        FilterRuleBuilder<Article, List<string>> builder) =>
        builder.For(article => article.Tags)
               .Operator<string>("containsAny",
                   (tags, value) => tags.Any(tag => tag == value))
               .Operator<string[]>("containsAll",
                   (tags, values) => values.All(v => tags.Contains(v)));
}
```

## Variations

- **Navigation paths.** `For(x => x.Department.Name)` is a valid accessor.
- **Computed accessors.** `For(u => u.FirstName + " " + u.LastName)`.
- **Operators without a value.** `.Operator("isEmpty", tags => tags.Count == 0)`.
- **Mixed argument types.** Chain `.Operator<string>(...)` and `.Operator<string[]>(...)` on the same builder.

## Typed values and the JSON resolver

Every operator that takes an argument deserializes it through System.Text.Json. A filter class with such an operator gets two constructors: a parameterless one that uses the reflection-based resolver, and one that accepts an `IJsonTypeInfoResolver`. For trimmed or Native AOT apps register the argument types on a `JsonSerializerContext`. See [Trim / AOT-clean setup](aot-clean-setup.md).

A rule that only has value-less operators needs no resolver.

## Pitfalls

- The method must be `static`, take one `FilterRuleBuilder<TEntity, TValue>`, and return `FilterRule<TEntity, TValue>`. Any accessibility works.
- A property cannot be carried by both `[Map]` and `[PropertyMap]`. Declaring both raises `FN0002`.
- Forgetting `For(...)`, or declaring the same operator name twice, throws `FilterConfigurationException` when the filter is constructed.
- The method runs once per filter instance. Do not put per-request logic in it.

## See also

- [Mapping properties](mapping-properties.md)
- [Building a definition by hand](hand-built-definitions.md)
