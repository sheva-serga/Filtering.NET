---
title: Diagnostics catalogue
description: 31 analyzer rules — 23 errors (FN0001–FN0023) and 8 warnings (FN1001–FN1008).
---

# Diagnostics catalogue

Filtering.Net ships 31 compile-time analyzer rules — 23 errors (`FN0001`–`FN0023`) and 8 warnings (`FN1001`–`FN1008`). Both `dotnet build` and the IDE surface them; the rule's `helpLinkUri` brings you back to this page.

## Errors

| Id | Rule | Trigger |
|----|------|---------|
| FN0001 | DuplicateMapping | Filter path is mapped by multiple sources (`[Map]`, `[PropertyMap]`, or `[MapNested]`) on the same filter class. |
| FN0002 | MapAndPropertyMapBoth | Property has both `[Map]` and `[PropertyMap]`. Use one or the other. |
| FN0003 | PropertyNotFound | Property does not exist on the entity type passed to `[GenerateFilter<T>]`. |
| FN0004 | IncompatibleProfile | Profile cannot be applied to the property's CLR type. |
| FN0005 | UnknownOperator | Operator name is not declared by the resolved profile. |
| FN0006 | MissingPartial | `[Map]` method is not declared `partial`. |
| FN0007 | NoInferableProfile | Property's CLR type has no built-in primitive profile; specify `Profile = typeof(...)` explicitly. |
| FN0008 | DuplicateInterceptor | Property has multiple `[InterceptValue]` declarations. |
| FN0009 | NonStaticOperator | `[FilterOperator]` member must be `public static`. |
| FN0010 | AliasCollision | Alias collides with another property or alias on the entity (case-insensitive). |
| FN0011 | InvalidBaseProfile | `[FilterProfile(BasedOn = typeof(X))]` references a type not marked with `[FilterProfile]`. |
| FN0012 | InterceptorWithoutMap | `[InterceptValue]` declared without a matching `[Map]` for the property. |
| FN0013 | AmbiguousProfile | Property's CLR type matches multiple profiles; use `[Map(typeof(...))]` to pick one. |
| FN0014 | ProfileMissingExtractor | Standalone `[FilterProfile]` is missing required extractor methods (`TryGetValue` / `TryGetArray`). |
| FN0015 | DuplicateOperatorOnProfile | Operator name is declared more than once on the same profile. |
| FN0016 | NestedCycle | `[MapNested]` introduces a cycle in the filter-inlining graph (e.g. `User.Manager: User`). |
| FN0017 | NestedCrossAssembly | `[MapNested<T>]` references a filter class declared outside the current compilation. |
| FN0018 | NestedAmbiguous | Auto-resolve `[MapNested(nameof(...))]` finds two or more `[GenerateFilter<TNav>]` candidates. |
| FN0019 | NestedTargetNotFound | Auto-resolve finds zero `[GenerateFilter<TNav>]` candidates for the navigation target type. |
| FN0020 | NestedNavigationInvalid | Named property doesn't exist, isn't a reference type, or is a primitive/value type. |
| FN0021 | NestedCollectionUnsupported | Named navigation is a collection type; collection navigations are deferred to a future version. |
| FN0022 | FilterClassHasBaseType | The `[GenerateFilter]` partial declares a base class. The generated part derives from `FilterDefinition<TEntity>`, so the class cannot have another base. |
| FN0023 | NestedMaxDepthInvalid | `[MapNested]` declares a negative `MaxDepth`. Use a positive value to bound the nesting, or omit it. |

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
