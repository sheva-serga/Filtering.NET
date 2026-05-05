---
title: Diagnostics catalogue
description: 25 analyzer rules — 17 errors (FN0001–FN0017) and 8 warnings (FN1001–FN1008).
---

# Diagnostics catalogue

Filtering.Net ships 25 compile-time analyzer rules — 17 errors (`FN0001`–`FN0017`) and 8 warnings (`FN1001`–`FN1008`). Each rule has its own page below explaining the trigger, the rationale, and the fix.

## Errors

| Id | Title | Summary |
|----|-------|---------|
| [FN0001](FN0001.md) | DuplicateMap | Property is mapped by multiple `[Map]` methods on the same filter class. |
| [FN0002](FN0002.md) | DuplicateSortable | Property is marked `Sortable = true` on multiple `[Map]` methods. |
| [FN0003](FN0003.md) | MapAndPropertyMapBoth | Property has both `[Map]` and `[PropertyMap]`. Use one or the other. |
| [FN0004](FN0004.md) | PropertyNotFound | Property does not exist on the entity type passed to `[GenerateFilter<T>]`. |
| [FN0005](FN0005.md) | IncompatibleProfile | Profile cannot be applied to the property's CLR type. |
| [FN0006](FN0006.md) | UnknownOperator | Operator name is not declared by the resolved profile. |
| [FN0007](FN0007.md) | InvalidValueConverter | `[ConvertWith]` type does not inherit from `ValueConverter<TModel, TProvider>`. |
| [FN0008](FN0008.md) | MissingPartial | `[Map]` method is not declared `partial`. |
| [FN0009](FN0009.md) | NoInferableProfile | Property's CLR type has no built-in primitive profile; specify `Profile = typeof(...)` explicitly. |
| [FN0010](FN0010.md) | DuplicateInterceptor | Property has multiple `[InterceptValue]` declarations. |
| [FN0011](FN0011.md) | NonStaticOperator | `[FilterOperator]` member must be `public static`. |
| [FN0012](FN0012.md) | AliasCollision | Alias collides with another property or alias on the entity (case-insensitive). |
| [FN0013](FN0013.md) | InvalidBaseProfile | `[FilterProfile(BasedOn = typeof(X))]` references a type not marked with `[FilterProfile]`. |
| [FN0014](FN0014.md) | InterceptorWithoutMap | `[InterceptValue]` declared without a matching `[Map]` for the property. |
| [FN0015](FN0015.md) | AmbiguousProfile | Property's CLR type matches multiple profiles; use `[Map(typeof(...))]` to pick one. |
| [FN0016](FN0016.md) | ProfileMissingExtractor | Standalone `[FilterProfile]` is missing required extractor methods (`TryGetValue` / `TryGetArray`). |
| [FN0017](FN0017.md) | DuplicateOperatorOnProfile | Operator name is declared more than once on the same profile. |

## Warnings

| Id | Title | Summary |
|----|-------|---------|
| [FN1001](FN1001.md) | DateTimeUtcNowInLambda | `DateTime.UtcNow` / `DateTime.Now` referenced directly inside a `[FilterOperator]` lambda. |
| [FN1002](FN1002.md) | NotSortableLikelyOmission | Property's CLR type is naturally sortable but `Sortable = true` is missing. |
| [FN1003](FN1003.md) | ProfileUnused | Filter profile is declared but never referenced by any `[Map]`. |
| [FN1004](FN1004.md) | OperatorUnused | Operator is declared on a profile but never reachable from any consumer. |
| [FN1005](FN1005.md) | ZeroOperatorsAllowed | `Only` / `Except` resolve to an empty operator set; filter leaves will always fail validation. |
| [FN1006](FN1006.md) | NullableNavInPath | Mapped path crosses a nullable navigation property. |
| [FN1007](FN1007.md) | UntranslatableMethodInOperator | Operator body uses a method that is not in the EF Core translatable allow-list. |
| [FN1008](FN1008.md) | FilterValueTypeUnregistered | Filter value type is not registered in any visible `JsonSerializerContext` (NativeAOT/trim setups). |
