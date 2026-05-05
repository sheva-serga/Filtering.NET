---
title: How it works
description: Compile-time generation flow from [GenerateFilter<T>] to executable predicates.
---

# How it works

## Compile-time, not runtime

Filtering.Net is a Roslyn incremental source generator. It runs in the analyzer process during `dotnet build` (and inside your IDE), walks every `[GenerateFilter<TEntity>]` partial class, and emits a sibling source file that contains the full `IFilterDefinition<TEntity>` implementation. The emitted code is plain C#: typed `Expression<Func<TEntity, bool>>` predicates per `(property, operator)` pair, typed `OrderBy` / `ThenBy` chains, and structured `Validate(...)` overloads.

There is **no** runtime expression-tree construction. The runtime never calls `Expression.Property`, `Expression.MakeBinary`, or any reflection-based predicate builder. Every typed predicate exists as a method in your compiled assembly before the first request arrives. EF Core's translator sees the same shape it would see if you wrote the predicates by hand.

## The two-pipeline architecture

The generator (`FilterGenerator.cs`) registers two `ForAttributeWithMetadataName` pipelines that run independently and emit to different targets:

1. **`[GenerateFilter<TEntity>]` branch.** Extracts a `FilterClassModel` per declared partial, reports per-class diagnostics (`FN0001`–`FN0017` errors, `FN1001`–`FN1002`, `FN1005`–`FN1007` warnings), and emits one source file per class. A collected view of all classes drives the assembly-wide `services.AddFiltering()` extension and the per-enum auto-emitted profiles.
2. **`[FilterProfile<T>]` branch.** Extracts profile-level models from custom profile classes and reports per-profile diagnostics like `FN0006` (operator missing), `FN0009` (no built-in match), `FN0014` (orphan interceptor), `FN1001` (`DateTime.UtcNow` in lambda), and `FN1002` (sortable omission).

Cross-pipeline diagnostics — `FN1003 ProfileUnused` and `FN1004 OperatorUnused` — join both `.Collect()` outputs so the generator can report a profile that no `[Map]` references, or an operator declared on a profile that no consumer uses.

## What the consumer sees

From the consumer's perspective, the flow per request is:

- Declare a `[GenerateFilter<TEntity>]` partial → the generator emits the implementation into `obj/`.
- The generated `services.AddFiltering()` extension registers every filter class as a singleton `IFilterDefinition<T>`.
- A controller or handler resolves `IFilterDefinition<T>` and calls `Apply(...)` (sync) or `ApplyPagedAsync(...)` (async EF helper).
- `Apply` runs `Validate(request)` first; on failure it throws `FilterValidationException` with a structured `FilterValidationResult`.
- On success, `Apply` invokes `ApplyFilter(queryable, where)` then `ApplySorting(queryable, sort, page, pageSize)` — both typed, both translatable.
- `ApplyPagedAsync` adds `CountAsync` + `ToListAsync` and packages into a `PageResult<T>`.

## See also

- [Profiles and operators](profiles-and-operators.md)
- [Validation philosophy](validation-philosophy.md)
- [The FilterRequest JSON shape](filter-request-shape.md)
