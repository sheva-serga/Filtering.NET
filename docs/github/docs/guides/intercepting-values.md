---
title: Intercepting filter values with [InterceptValue]
description: Transform typed values before predicate building.
---

# Intercepting filter values with `[InterceptValue]`

## What this does

`[InterceptValue(nameof(SomeProperty))]` decorates a method that runs once per leaf value after deserialization but before predicate building. The interceptor receives an `InterceptContext` (carrying the field name and operator) plus the typed value, and returns the transformed value. Use it to normalize input — lowercase emails, trim whitespace, parse user-friendly date phrases — without leaking that logic into every predicate.

## When to use

- **Normalization** — lowercase emails, strip leading/trailing whitespace, collapse internal whitespace.
- **Aliased input shapes** — accept "today" / "yesterday" strings and convert to `DateTime` cutoffs.
- **Symmetric SQL** — when the column is stored lowercase, lowercase the value before building the predicate so the SQL stays a clean equality check (rather than the predicate calling `column.ToLower() == value.ToLower()` and losing the index).

## Minimal code

Lifted from the sample app's `UserFilter.cs`:

```csharp
[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Email), Profile = typeof(StringFilter), Sortable = true,
        Only = new[] { "eq", "contains", "isNull" })]
    private static partial void MapEmail();

    // [InterceptValue] runs once per leaf value before predicate building.
    // Must be 'internal' or 'public' — the per-property class is a file-scoped
    // compilation unit, so a 'private' method is invisible from the emitted code.
    [InterceptValue(nameof(User.Email))]
    internal static string NormalizeEmail(InterceptContext context, string value) =>
        value.ToLowerInvariant();
}
```

A request with `{ "field": "email", "op": "eq", "value": "Alice@Example.com" }` ends up running `WHERE email = 'alice@example.com'` — no `LOWER(...)` wrapper, the index works, and you don't have to hand-normalize at every call site.

## Variations

- Per-operator branching — `InterceptContext.Operator` carries the operator name. An interceptor can transform the value differently for `eq` versus `contains` (e.g. trim whitespace for both, but also lowercase for `eq`).
- Raw JSON mode — `[InterceptValue(nameof(...), Raw = true)]` makes the method receive the raw `JsonElement` and return the typed value, replacing the built-in deserialization. Useful for accepting user-friendly date strings ("today") that the JSON deserializer wouldn't otherwise parse.
- Array-shaped operators — for operators with an array value (`in`), declare a sibling interceptor with a `string[]` (or appropriate array) parameter; the generator wires it to the array shape.

## Pitfalls

- The interceptor method must be `internal` or `public`. The generator emits each property's dispatch logic into a `file`-scoped class (its own compilation unit), so a `private` interceptor is invisible from the emitted code and produces a CS-error at consumer-build time.
- Only one `[InterceptValue]` per property is allowed. A second one fires `FN0009`.
- An `[InterceptValue]` whose property name does not match any `[Map]` on the same class raises `FN0013` — orphan interceptors are almost always typos.

## See also

- [Mapping properties](mapping-properties.md)
