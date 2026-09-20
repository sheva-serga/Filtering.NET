---
title: Intercepting filter values with [InterceptValue]
description: Transform typed values before predicate building.
---

# Intercepting filter values with `[InterceptValue]`

## What this does

`[InterceptValue(nameof(SomeProperty))]` decorates a static method that runs once per leaf value, after the JSON value is parsed and before the predicate is built. The interceptor receives an `InterceptContext` carrying the property path, the field name the client used, and the operator. It returns the transformed value. Use it to normalize input without leaking that logic into every predicate.

## When to use

- **Normalization.** Lowercase emails, trim whitespace, collapse internal whitespace.
- **Aliased input shapes.** Accept "today" or "yesterday" and convert to `DateTime` cutoffs.
- **Symmetric SQL.** When the column is stored lowercase, lowercase the value so the SQL stays a plain equality check and the index keeps working.

## Minimal code

From the sample app's `UserFilter.cs`:

```csharp
[GenerateFilter<User>]
[Map(nameof(User.Email), Profile = typeof(StringFilter), Sortable = true, Only = new[] { "eq", "contains", "isNull" })]
public partial class UserFilter
{
    [InterceptValue(nameof(User.Email))]
    private static string NormalizeEmail(InterceptContext context, string value) =>
        value.ToLowerInvariant();
}
```

A request with `{ "field": "email", "op": "eq", "value": "Alice@Example.com" }` runs `WHERE email = 'alice@example.com'`.

## Variations

- **Per-operator branching.** `InterceptContext.Operator` carries the operator name, so one interceptor can treat `eq` and `contains` differently.
- **Array values.** An interceptor whose value parameter is an array, for example `string[]`, applies to array operators such as `in`.
- **Raw JSON mode.** `[InterceptValue(nameof(...), Raw = true)]` makes the method receive the raw `JsonElement` and return the typed value, replacing the built-in parsing for scalar operators.
- **Rejecting a value.** Throw `FilterValidationException` from the interceptor. Validation reports it as `InterceptorRejected` with your first error message.

## Pitfalls

- The method must be `static`. Any accessibility works, including `private`.
- Only one `[InterceptValue]` per property is allowed. A second one fires `FN0007`.
- An `[InterceptValue]` whose property name matches no `[Map]` on the same class raises `FN0011`.
- Interceptors apply to values parsed by the profile. Custom operators with typed values, which are deserialized through the JSON resolver, are not intercepted.
- Interceptors run during validation as well as during filtering, so keep them cheap and free of side effects.

## See also

- [Mapping properties](mapping-properties.md)
- [Nested filters](nested-filters.md). Interceptors on a nested filter keep running on the host.
