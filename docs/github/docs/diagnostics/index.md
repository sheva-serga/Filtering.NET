---
title: Diagnostics catalogue
description: 24 analyzer rules — 16 errors (FN0001–FN0016) and 8 warnings (FN1001–FN1008).
---

# Diagnostics catalogue

Filtering.Net ships 24 compile-time analyzer rules — 16 errors (`FN0001`–`FN0016`) and 8 warnings (`FN1001`–`FN1008`). Both `dotnet build` and the IDE surface them; the rule's `helpLinkUri` brings you back to this page.

## Errors

| Id | Rule | Trigger |
|----|------|---------|
| FN0001 | DuplicateMap | Property is mapped by multiple `[Map]` methods on the same filter class. |
| FN0002 | DuplicateSortable | Property is marked `Sortable = true` on multiple `[Map]` methods. |
| FN0003 | MapAndPropertyMapBoth | Property has both `[Map]` and `[PropertyMap]`. Use one or the other. |
| FN0004 | PropertyNotFound | Property does not exist on the entity type passed to `[GenerateFilter<T>]`. |
| FN0005 | IncompatibleProfile | Profile cannot be applied to the property's CLR type. |
| FN0006 | UnknownOperator | Operator name is not declared by the resolved profile. |
| FN0007 | MissingPartial | `[Map]` method is not declared `partial`. |
| FN0008 | NoInferableProfile | Property's CLR type has no built-in primitive profile; specify `Profile = typeof(...)` explicitly. |
| FN0009 | DuplicateInterceptor | Property has multiple `[InterceptValue]` declarations. |
| FN0010 | NonStaticOperator | `[FilterOperator]` member must be `public static`. |
| FN0011 | AliasCollision | Alias collides with another property or alias on the entity (case-insensitive). |
| FN0012 | InvalidBaseProfile | `[FilterProfile(BasedOn = typeof(X))]` references a type not marked with `[FilterProfile]`. |
| FN0013 | InterceptorWithoutMap | `[InterceptValue]` declared without a matching `[Map]` for the property. |
| FN0014 | AmbiguousProfile | Property's CLR type matches multiple profiles; use `[Map(typeof(...))]` to pick one. |
| FN0015 | ProfileMissingExtractor | Standalone `[FilterProfile]` is missing required extractor methods (`TryGetValue` / `TryGetArray`). |
| FN0016 | DuplicateOperatorOnProfile | Operator name is declared more than once on the same profile. |

## Warnings

| Id | Rule | Trigger |
|----|------|---------|
| FN1001 | DateTimeUtcNowInLambda | `DateTime.UtcNow` / `DateTime.Now` referenced directly inside a `[FilterOperator]` lambda. |
| FN1002 | NotSortableLikelyOmission | Property's CLR type is naturally sortable but `Sortable = true` is missing. |
| FN1003 | ProfileUnused | Filter profile is declared but never referenced by any `[Map]`. |
| FN1004 | OperatorUnused | Operator is declared on a profile but never reachable from any consumer. |
| FN1005 | ZeroOperatorsAllowed | `Only` / `Except` resolve to an empty operator set; filter leaves will always fail validation. |
| FN1006 | NullableNavInPath | Mapped path crosses a nullable navigation property. |
| FN1007 | UntranslatableMethodInOperator | Operator body uses a method that is not in the EF Core translatable allow-list. |
| FN1008 | FilterValueTypeUnregistered | Filter value type is not registered in any visible `JsonSerializerContext` (NativeAOT/trim setups). |
