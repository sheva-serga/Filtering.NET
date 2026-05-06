---
title: Validation philosophy
description: Validate before EF Core sees the request — return structured errors, not exceptions.
---

# Validation philosophy

## Validate-then-execute

Every call to `Apply(...)` or `ApplyPagedAsync(...)` runs `Validate(request)` first. The implementation walks the `where` tree, the `sort` list, and the `(page, pageSize)` pair against the generated definition's metadata and accumulates every problem it finds — it does not stop at the first error.

When the resulting `FilterValidationResult.IsValid` is `false`, the orchestrator throws `FilterValidationException` whose `Result` property carries the full list. The underlying `IQueryable<T>` is never touched. EF Core never sees a malformed request, so you never get a 500 from an SQL translation error caused by a bad client payload.

## Structured errors with paths

Each `FilterValidationError` has three fields:

- **`Code`** — a `FilterValidationCode` enum value (see the list below).
- **`Path`** — a JSON-pointer-style path into the request (`/where/and/0/operator`, `/sort/1/dir`, `/pageSize`). Clients use it to highlight the offending field.
- **`Message`** — a human-readable description.

A response payload after a controller maps `invalid.Result` to `BadRequest` looks like:

```json
{
  "errors": [
    { "code": "OperatorNotAllowed", "path": "/where/and/0/operator",
      "message": "Operator 'fuzzy' is not allowed on field 'Name'." },
    { "code": "UnknownField", "path": "/where/and/1/field",
      "message": "Field 'isActive2' is not configured for filtering." }
  ]
}
```

## Codes you'll see at runtime

`FilterValidationCode` (in `src/Filtering.Net/Validation/FilterValidationCode.cs`) enumerates every condition the validator can report:

- **`UnknownField`** — field name not configured for filtering.
- **`OperatorNotAllowed`** — operator not in the property's profile, or excluded by `Only` / `Except`.
- **`InvalidValueType`** — wrong `JsonValueKind` (e.g., bool where number expected).
- **`InvalidValueFormat`** — right kind, wrong format (e.g., `"abc"` for an invariant decimal).
- **`EmptyInArray`** — `in` operator with an empty array.
- **`InterceptorRejected`** — an `[InterceptValue]` method threw `FilterValidationException`.
- **`NotSortable`** — sort field is not configured as `Sortable = true`.
- **`InvalidSortDirection`** — `dir` value not `Asc` or `Desc`.
- **`PageInvalid`** — `page < 1`.
- **`PageSizeTooLarge`** — `pageSize > MaxPageSize`.
- **`PageSizeInvalid`** — `pageSize < 1`.
- **`NestingTooDeep`** — filter nesting depth exceeds `MaxNestingDepth`.
- **`TooManyConditions`** — total leaf count exceeds `MaxLeafConditions`.
- **`GroupEmpty`** — `and: []` or `or: []` with zero children.

## How to surface this in HTTP APIs

The canonical controller pattern is a single `try` / `catch` around the apply call:

```csharp
catch (FilterValidationException invalid)
{
    return BadRequest(invalid.Result);
}
```

`invalid.Result` serializes as the `FilterValidationResult` JSON shape shown above. If you prefer RFC 7807 ProblemDetails, map the codes to `errors`-keyed extensions in middleware — see the [handling validation errors](../guides/handling-validation-errors.md) guide for a full walkthrough.

!!! note
    `FilterValidationException` is the only exception `Apply` / `ApplyPagedAsync` throw for client-payload problems. Configuration errors (e.g., a missing DI registration) surface as `FilterDispatchException` or `FilterConfigurationException` and should be treated as 500s — they indicate a bug in your wiring, not a bad request.

## See also

- [Handling validation errors](../guides/handling-validation-errors.md)
- [The FilterRequest JSON shape](filter-request-shape.md)
