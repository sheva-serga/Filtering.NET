---
title: Diagnostics catalogue
description: 37 analyzer rules — 29 errors (FN0001–FN0029) and 8 warnings (FN1001–FN1008).
---

# Diagnostics catalogue

Filtering.Net ships 37 compile-time analyzer rules — 29 errors (`FN0001`–`FN0029`) and 8 warnings (`FN1001`–`FN1008`). Both `dotnet build` and the IDE surface them; the rule's `helpLinkUri` brings you back to this page.

## Errors

| Id | Rule | Trigger |
|----|------|---------|
| FN0001 | DuplicateMapping | Filter path is mapped by multiple sources (`[Map]`, `[PropertyMap]`, or `[MapNested]`) on the same filter class. |
| FN0002 | MapAndPropertyMapBoth | Property has both `[Map]` and `[PropertyMap]`. Use one or the other. |
| FN0003 | PropertyNotFound | Property does not exist on the entity type passed to `[GenerateFilter<T>]`. |
| FN0004 | IncompatibleProfile | Profile cannot be applied to the property's CLR type. |
| FN0005 | UnknownOperator | Operator named in `[Map(Only = ...)]` / `[Map(Except = ...)]` is not declared by the resolved profile. |
| FN0006 | NoInferableProfile | Property's CLR type has no built-in primitive profile; specify `Profile = typeof(...)` explicitly. |
| FN0007 | DuplicateInterceptor | Property has multiple `[InterceptValue]` declarations. |
| FN0008 | NonStaticOperator | `[FilterOperator]` member must be `public static`. |
| FN0009 | AliasCollision | Alias collides with another property or alias on the entity (case-insensitive). |
| FN0010 | InvalidBaseProfile | `[FilterProfile(BasedOn = typeof(X))]` references a type not marked with `[FilterProfile]`. |
| FN0011 | InterceptorWithoutMap | `[InterceptValue]` declared without a matching `[Map]` for the property. |
| FN0012 | AmbiguousProfile | Property's CLR type matches multiple profiles; use `[Map(typeof(...))]` to pick one. |
| FN0013 | DuplicateOperatorOnProfile | Operator name is declared more than once on the same profile (names are compared case-insensitively). |
| FN0014 | NestedCycle | `[MapNested]` graph contains a cycle in which no nesting declares `MaxDepth` (e.g. `User.Manager: User`). Set `MaxDepth` on one of them to allow it. |
| FN0015 | NestedFilterClassUnusable | `[MapNested<T>]` does not name a `[GenerateFilter<TNavigation>]` partial in this compilation — `T` is not a filter class, targets a different entity, or lives in a referenced assembly. |
| FN0016 | NestedAmbiguous | Auto-resolve `[MapNested(nameof(...))]` finds two or more `[GenerateFilter<TNav>]` candidates. |
| FN0017 | NestedTargetNotFound | Auto-resolve finds zero `[GenerateFilter<TNav>]` candidates for the navigation target type. |
| FN0018 | NestedNavigationInvalid | Named property doesn't exist, isn't a reference type, or is a primitive/value type. |
| FN0019 | NestedCollectionUnsupported | Named navigation is a collection type; collection navigations are deferred to a future version. |
| FN0020 | FilterClassHasBaseType | The `[GenerateFilter]` partial declares a base class. The generated part derives from `FilterDefinition<TEntity>`, so the class cannot have another base. |
| FN0021 | NestedMaxDepthInvalid | `[MapNested]` declares a `MaxDepth` outside 1–64. Use a value in range to bound the nesting, or omit it. |
| FN0022 | ProfileTypeNotAProfile | `[Map(Profile = typeof(X))]` references a type not marked with `[FilterProfile<TColumn>]`. |
| FN0023 | PropertyMapSignatureInvalid | `[PropertyMap]` method is not `static`, does not take exactly one `FilterRuleBuilder<TEntity, TValue>`, or does not return `FilterRule<TEntity, TValue>`. |
| FN0024 | NullableValueTypeInPath | A dotted `[Map]` path reads a member through a `Nullable<T>` segment (e.g. `"Created.Year"` on a `DateTime?`), which does not expose it. |
| FN0025 | NestedPrefixBlank | `[MapNested(Prefix = "")]` (or whitespace). Omit `Prefix` to dispatch under the navigation name. |
| FN0026 | NestedPathFilterUnknown | A dotless `[MapNested]` `Only` / `Except` entry names a path the nested filter class does not map. |
| FN0027 | FilterClassPlacementInvalid | The `[GenerateFilter]` partial is nested in another type or declares type parameters. The generated half is a top-level partial in the class's namespace. |
| FN0028 | OperatorMemberShapeInvalid | A `[FilterOperator]` member is not an `Expression<Func<TColumn, bool>>` / `Expression<Func<TColumn, TValue, bool>>` template, so no operator can be built from it. |
| FN0029 | InterceptorSignatureInvalid | `[InterceptValue]` method is not `static`, does not take `(InterceptContext, TValue)`, or does not return the value type it receives (`JsonElement` for `Raw = true`). |

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
