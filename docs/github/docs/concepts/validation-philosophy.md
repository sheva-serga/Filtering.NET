---
title: Validation philosophy
description: Validate before EF Core sees the request — return structured errors, not exceptions.
---

# Validation philosophy

## Validate-then-execute

Every call to `Apply(...)` or `ApplyPagedAsync(...)` runs `Validate(request)` first. The implementation walks the `where` tree, the `sort` list, and the `(page, pageSize)` pair against the generated definition's metadata and accumulates every problem it finds — it does not stop at the first error.

When the resulting `FilterValidationResult.IsValid` is `false`, the orchestrator throws `FilterValidationException` whose `Result` property carries the full list. The underlying `IQueryable<T>` is never touched. EF Core never sees a malformed request, so you never get a 500 from an SQL translation error caused by a bad client payload.

## Structured errors with paths

Each `FilterValidationError` carries three fields that are always populated:

- **`Code`** — a `FilterValidationCode` enum value (see the list below).
- **`Path`** — a dotted path into the request. The filter tree is rooted at `where`, each group appends its combinator and the child index (`where.and[0]`, `where.or[1].not[0]`), and a leaf error appends the member that failed (`.op` or `.value`). Sort errors are `sort[<index>].field` / `sort[<index>].dir`, and paging errors are the bare `page` / `pageSize`. Clients use the path to highlight the offending field.
- **`Message`** — a human-readable description.

`FilterValidationError` also carries `Field` and `OperatorName` for leaf-level errors, so a client can key on those instead of parsing the path.

A response payload after a controller maps `invalid.Result` to `BadRequest` looks like:

```json
{
  "isValid": false,
  "errors": [
    { "path": "where.and[0].op", "code": "OperatorNotAllowed",
      "message": "Operator 'fuzzy' is not supported on field 'Name'.",
      "field": "Name", "operatorName": "fuzzy" },
    { "path": "where.and[1]", "code": "UnknownField",
      "message": "Field 'isActive2' is not configured for filtering.",
      "field": "isActive2" }
  ]
}
```

!!! note
    `Code` is a plain C# enum and the library registers no converter for it, so System.Text.Json writes it as its numeric value. Register `JsonStringEnumConverter` on your app's `JsonSerializerOptions` if you want the names shown above on the wire.

## Codes you'll see at runtime

`FilterValidationCode` (in `src/Filtering.Net/Validation/FilterValidationCode.cs`) enumerates every condition the validator can report:

- **`UnknownField`** — field name not configured for filtering.
- **`OperatorNotAllowed`** — operator not in the property's profile, or excluded by `Only` / `Except`.
- **`InvalidValueType`** — the value could not be read for the operator: a wrong `JsonValueKind` (bool where a number was expected, a non-array for `in`), a string that does not parse (`"abc"` for a decimal, a bad GUID or enum name), a value sent to an operator that takes none, or a JSON `null` for an operator that takes a typed value.
- **`InterceptorRejected`** — an `[InterceptValue]` method threw `FilterValidationException`.
- **`NotSortable`** — sort field is missing, unknown, or not configured as `Sortable = true`.
- **`InvalidSortDirection`** — `dir` value is outside the `SortDir` enum (not `Asc` or `Desc`).
- **`PageInvalid`** — `page < 1`, or a `page` so large that the rows it skips do not fit in an `int`.
- **`PageSizeTooLarge`** — `pageSize > MaxPageSize`.
- **`PageSizeInvalid`** — `pageSize < 1`.
- **`NestingTooDeep`** — filter nesting depth exceeds `MaxNestingDepth` (assembly-level `[FilterDefaults]`, default 10).
- **`TooManyConditions`** — total leaf count exceeds `MaxLeafConditions` (assembly-level `[FilterDefaults]`, default 50).
- **`GroupEmpty`** — `and: []`, `or: []`, or `not: []` with zero children.
- **`InvalidNodeShape`** — a node the engine cannot interpret: a `not` group with a child count other than one, a combinator outside `LogicalOp`, or a `FilterNode` subtype that is neither `FilterGroup` nor `FilterLeaf`.

Two members of the enum, `InvalidValueFormat` and `EmptyInArray`, are declared but never produced: a malformed value is reported as `InvalidValueType`, and an empty `in` array is accepted (it matches nothing). Don't branch on them.

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
