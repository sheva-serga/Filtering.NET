---
title: Nested filter inlining
description: Inline another [GenerateFilter] partial's mappings into a host filter under a dotted prefix with [MapNested].
---

# Nested filter inlining

## What this does

`[MapNested(nameof(User.Department))]` tells the source generator to inline another `[GenerateFilter<TNav>]` partial's mappings (every `[Map]`, `[PropertyMap]`, `[InterceptValue]`, `[FilterValidator]`, `[FilterOperator]`) into the host filter under a dotted prefix (default: the navigation property name in PascalCase). At runtime, the host's emitted dispatch table looks identical to one where every nav column was declared with `[Map("Department.X")]` by hand.

## When to use

- Your entity has a related entity whose `[GenerateFilter<TRelated>]` partial already maps every filterable column you want to expose.
- You want `Only` / `Except` / `Prefix` / `DisableSorting` to compose with the related filter's existing setup.
- The wire format stays flat — clients still send `field: "department.name"`.

## Minimal code

```csharp
[GenerateFilter<Department>]
public partial class DepartmentFilter
{
    [Map(nameof(Department.Id), Sortable = true)]   private static partial void MapId();
    [Map(nameof(Department.Name), Sortable = true)] private static partial void MapName();
}

[GenerateFilter<User>]
public partial class UserFilter
{
    [Map(nameof(User.Name))]                        private static partial void MapName();
    [MapNested(nameof(User.Department))]            private static partial void MapDepartment();
}
```

Wire fields exposed by `UserFilter`: `name`, `department.id`, `department.name`. Wire keys are case-insensitive at dispatch.

## Auto-resolve vs explicit

- `[MapNested(nameof(User.Department))]` — auto-resolves the unique `[GenerateFilter<Department>]` partial in the compilation. Two candidates → `FN0018`.
- `[MapNested<DepartmentFilter>(nameof(User.Department))]` — explicit, type-checked at the call site. Use when there are multiple filter classes for the same entity.

## Configuration knobs

- `Prefix = "dept"` — overrides the default prefix on the wire (CLR navigation path is unaffected).
- `Only = new[] { "Id", "Name" }` — restricts the merged paths to this allow-list (paths are relative to the nested filter, in CLR PascalCase).
- `Except = new[] { "InternalNotes" }` — drops these paths from the merge.
- `DisableSorting = true` — every merged column is non-sortable through this nesting, regardless of the source's `Sortable` setting.

## Transitive nesting

Merge is recursive: if `DepartmentFilter` itself has `[MapNested(nameof(Department.Company))]`, then `UserFilter` exposes `department.company.*` paths automatically. Cycles are caught at compile time as `FN0016 NestedCycle`.

## v1 limitations

- `[InterceptValue]` and `[PropertyMap]` overrides on the source filter do **not** propagate through `[MapNested]` splice in v1 — the spliced host calls into raw column accessors, not through the source filter's wrappers. Splice-through for these is a future-version follow-up.
- Cross-assembly is unsupported in v1 — the target filter class must live in the same compilation. `FN0017 NestedCrossAssembly`.
- Collection navigations (`User.Posts: List<Post>`) are unsupported in v1 — `FN0021 NestedCollectionUnsupported`. Reference navigations only.
- Duplicate paths across `[Map]`, `[PropertyMap]`, and `[MapNested]` produce `FN0001 DuplicateMapping` with all conflicting locations reported.

## See also

- [Navigation paths and aliases](navigation-paths.md) — the per-column form.
- [Diagnostics catalogue](../diagnostics/index.md) — `FN0016`–`FN0021`.
