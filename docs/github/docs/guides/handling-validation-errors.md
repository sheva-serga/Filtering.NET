---
title: Handling validation errors (HTTP 400 mapping)
description: Convert FilterValidationException into a structured 400 response.
---

# Handling validation errors (HTTP 400 mapping)

## What this does

Both `IQueryable<T>.Apply(...)` and `ApplyPagedAsync(...)` validate the request first. On failure they throw `FilterValidationException`, whose `.Result` property is a `FilterValidationResult` — a record carrying `IsValid` plus a list of `FilterValidationError` (each with `Path`, `Code`, `Message`, `Field`, `OperatorName`). The controller catches the exception and converts the structured result into an HTTP 400 response payload.

## When to use

Every endpoint that accepts an external `FilterRequest` payload. Validation failures are caller-driven (bad field name, unsupported operator, malformed value) and deserve a 400; they should never become a 500.

## Minimal code

Pattern from `samples/UserManagement.WebApi/Controllers/UsersController.cs`:

```csharp
[HttpPost("search")]
public async Task<ActionResult<PageResult<User>>> SearchAsync(
    [FromBody] FilterRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var pageResult = await _dbContext.Users
            .ApplyPagedAsync(_userFilter, request, cancellationToken);

        return Ok(pageResult);
    }
    catch (FilterValidationException invalid)
    {
        // invalid.Result is a FilterValidationResult with .Errors
        // (Path, Code, Message, Field, OperatorName).
        return BadRequest(invalid.Result);
    }
}
```

The `FilterValidationResult` serializes to JSON cleanly, so passing it straight to `BadRequest(...)` produces a structured response that clients can parse. An example payload:

```json
{
  "isValid": false,
  "errors": [
    {
      "path": "where.and[0].value",
      "code": "InvalidValueType",
      "message": "Expected number for operator 'gt' on field 'age'.",
      "field": "age",
      "operatorName": "gt"
    }
  ]
}
```

## Variations

- **Global exception filter** — instead of a try/catch in every controller action, register an ASP.NET Core `IExceptionFilter` that maps `FilterValidationException` to a 400 with `invalid.Result` as the body. Keeps controller methods clean. (The implementation is a few lines — outside the scope of this guide.)
- **Custom error envelope** — wrap `invalid.Result.Errors` inside your project's standard error shape (e.g. `{ "type": "validation", "details": [...] }`) before returning. The structured fields make the mapping mechanical.
- **Pre-validate without throwing** — for endpoints that report validation status without executing (the sample's `/users/validate`), call `_userFilter.Validate(request)` directly and return the `FilterValidationResult`; no exception is thrown.

## Pitfalls

- The `catch` must be specific to `FilterValidationException`. **Never** catch the broader `Exception` and return 400 — that hides genuine bugs (database failures, NREs, programming errors) behind a misleading status code.
- Don't swallow `OperationCanceledException` in the same catch block. Cancellation is not a validation failure; it's the client disconnecting. Let it propagate so the host can short-circuit the response.
- The `FilterValidationCode` enum (`UnknownField`, `OperatorNotAllowed`, `InvalidValueType`, `InvalidValueFormat`, `EmptyInArray`, `InterceptorRejected`, `NotSortable`, `InvalidSortDirection`, `PageInvalid`, `PageSizeTooLarge`, `PageSizeInvalid`, `NestingTooDeep`, `TooManyConditions`, `GroupEmpty`) is the canonical list — UI translations and per-error help links should switch on these codes, not on the human-readable message.

## See also

- [Validation philosophy](../concepts/validation-philosophy.md)
