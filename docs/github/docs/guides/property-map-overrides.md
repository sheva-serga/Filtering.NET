---
title: Per-property override with [PropertyMap]
description: Replace generated predicate logic per-property using a fluent For/Operator DSL.
---

# Per-property override with `[PropertyMap]`

## What this does

`[PropertyMap]` is a per-property escape hatch that bypasses the profile system entirely. Instead of picking operators from a profile, you write a fluent `builder.For(x => x.Path).Operator<TArg>("name", (column, value) => ...)` chain that the source generator parses at compile time. Each `Operator` call becomes a typed leaf method in the emitted filter class — no runtime lambda evaluation.

## When to use

Anything the profile / operator system can't express cleanly:

- Array-typed columns where you want `containsAny` / `containsAll` semantics over `List<T>` or `T[]`.
- JSONB columns where the predicate calls EF Core's JSON helpers.
- Computed columns whose accessor is a non-trivial expression (`u => u.FirstName + " " + u.LastName`).
- Full-text wrappers that pass through provider-specific functions.

For "I want to add one operator to a CLR type's normal surface", prefer a `[FilterProfile<T>]` with `BasedOn = ...` — it's reusable across many properties. Reach for `[PropertyMap]` when the override is genuinely property-specific.

## Minimal code

```csharp
using System.Collections.Generic;
using System.Linq;
using Filtering.Net;

public sealed class Article
{
    public int Id { get; set; }
    public List<string> Tags { get; set; } = new();
}

[GenerateFilter<Article>]
public partial class ArticleFilter
{
    [Map(nameof(Article.Id), Sortable = true)]
    private static partial void MapId();

    // Tags is a List<string>. No built-in profile covers collection columns, so we override.
    // The body is parsed at compile time — the For(...) and Operator<T>(...) calls are
    // syntactic markers, not runtime calls. The chain inlines two typed leaf methods:
    // one for "containsAny" and one for "containsAll".
    [PropertyMap(nameof(Article.Tags))]
    private static FilterRule<Article, List<string>> MapTags(
        FilterRuleBuilder<Article, List<string>> builder) =>
        builder.For(article => article.Tags)
               .Operator<string>("containsAny",
                   (List<string> tags, string value) => tags.Any(tag => tag == value))
               .Operator<string[]>("containsAll",
                   (List<string> tags, string[] values) => values.All(v => tags.Contains(v)));
}
```

The contrast with `[Map]`: a `[Map]` method is empty and points at a profile that defines operators uniformly across many columns. A `[PropertyMap]` method has a body that names every operator inline, with hand-written predicates — perfect for one-off shapes that don't generalize.

## Variations

- Navigation paths — `For(x => x.Department.Name)` is a valid accessor expression. The generator inlines the dotted path.
- Computed accessors — `For(u => u.FirstName + " " + u.LastName)` produces a typed leaf whose column expression is the full computation.
- Mixing operator argument shapes — chain `.Operator<string>(...)` and `.Operator<string[]>(...)` on the same builder; each emits its own leaf with a typed value parameter.

## Pitfalls

- A property cannot be carried by both `[Map]` and `[PropertyMap]`. Declaring both raises `FN0003`.
- Operator names typed in `For(...).Operator(...)` chains are validated against the operator dispatch surface; misspelt names fire `FN0006`.
- Calls to `FilterRuleBuilder.For` / `Operator` at runtime throw `FilterConfigurationException`. They are compile-time syntactic markers — the generator parses them and emits the implementation.

## See also

- [Mapping properties](mapping-properties.md)
- [PropertyMap attribute reference](../reference/attributes/property-map.md)
- [FN0003 — property has both `[Map]` and `[PropertyMap]`](../reference/diagnostics/FN0003.md)
