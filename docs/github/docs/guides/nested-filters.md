---
title: Nested filters
description: Reuse another [GenerateFilter] partial's mappings under a dotted prefix with [MapNested].
---

# Nested filters

## What this does

`[MapNested(nameof(User.Department))]` makes the host filter reuse another `[GenerateFilter<TNav>]` partial. When the host is constructed it takes the nested filter's schema and re-roots every property on the host entity: the accessor `department => department.Name` becomes `user => user.Department.Name`, and the wire key gains the prefix.

Everything the source filter configured comes along: `Sortable`, `Alias`, `Only` / `Except`, custom profile operators, `[InterceptValue]` methods, and `[PropertyMap]` rules.

## When to use

- A related entity already has a `[GenerateFilter<TRelated>]` partial that maps the columns you want to expose.
- You want `Only` / `Except` / `Prefix` / `DisableSorting` to compose with that filter's existing setup.
- The wire format should stay flat. Clients still send `field: "department.name"`.

## Minimal code

```csharp
[GenerateFilter<Department>]
[Map(nameof(Department.Id), Sortable = true)]
[Map(nameof(Department.Name), Sortable = true)]
public partial class DepartmentFilter { }

[GenerateFilter<User>]
[Map(nameof(User.Name))]
[MapNested(nameof(User.Department))]
public partial class UserFilter { }
```

Wire fields exposed by `UserFilter`: `name`, `department.id`, `department.name`. Wire keys are matched case-insensitively.

## Auto-resolve vs explicit

- `[MapNested(nameof(User.Department))]` resolves the unique `[GenerateFilter<Department>]` partial in the compilation. Two candidates raise `FN0016`; none raises `FN0017`.
- `[MapNested<DepartmentFilter>(nameof(User.Department))]` names the filter class. Use it when several filter classes target the same entity.

## Configuration knobs

- `Prefix = "dept"` changes the prefix on the wire. The CLR path `Department.Name` stays valid as a key too. Omit `Prefix` to dispatch under the navigation name; a present-but-blank `Prefix` is the compile error `FN0025`.
- `Only = new[] { "Id", "Name" }` keeps only these paths. Paths are relative to the nested filter, in CLR PascalCase. A dotless entry that the nested filter class does not map is `FN0026`; entries for paths that the nested filter itself gets from a further `[MapNested]` must be spelled with a dot.
- `Except = new[] { "InternalNotes" }` drops these paths, under the same rules.
- `DisableSorting = true` makes every property that arrives through this nesting non-sortable.
- `MaxDepth = 2` bounds how often this nesting is followed along one path. See the next section.

## Transitive nesting

If `DepartmentFilter` itself has `[MapNested(nameof(Department.Company))]`, then `UserFilter` exposes `department.company.*` as well. `Only` and `Except` apply to the nested filter's own mappings. Properties it nests in turn follow their own `[MapNested]` settings.

## Self-referencing and circular models

Entity models are often circular: `Employee.Manager` points at another `Employee`, or `User.Department` and `Department.Head` point at each other. Nesting such a graph without a limit would never end, so an unbounded cycle is the compile error `FN0014 NestedCycle`. Give at least one nesting in the cycle a `MaxDepth` and the cycle becomes legal:

```csharp
[GenerateFilter<Employee>]
[Map(nameof(Employee.Name), Sortable = true)]
[MapNested(nameof(Employee.Manager), MaxDepth = 2)]
public partial class EmployeeFilter { }
```

This exposes `name`, `manager.name`, and `manager.manager.name`. A request for `manager.manager.manager.name` fails validation with `UnknownField`. On EF Core each level becomes one self-join.

`MaxDepth = N` means this particular `[MapNested]` is followed at most N times along any single path. In a cycle across several filters, the expansion stops where the bounded nesting runs out:

```csharp
// UserFilter:       [MapNested(nameof(User.Department))]
// DepartmentFilter: [MapNested(nameof(Department.Head), MaxDepth = 1)]
```

`UserFilter` then exposes `department.*`, `department.head.*`, and `department.head.department.*`, and stops there because `Head` was already followed once.

Keep `MaxDepth` small. Every level adds a join to queries that use it, and the number of exposed fields grows with each level.

## Nullable navigations

A navigation may be nullable — `User.Department` declared as `Department?`. Nesting through one is supported and raises no diagnostic: the lifted accessor is a plain member chain (`user => user.Department.Name`), and the query provider decides what a missing related row means. On EF Core that is a LEFT JOIN whose predicate simply does not match, so rows with no related entity fall out of the result. There is no per-property null guard to add on this path, and `FN1006` — which does fire for a dotted `[Map]` across a nullable navigation — is deliberately not raised here.

If you need rows with a missing navigation to be *included*, express that in the request rather than the mapping: an `or` group with an `isNull` leaf on the foreign key alongside the nested condition.

## Typed operator values

If the nested filter, or anything it nests, has an operator that takes a typed value, the host also gets the `IJsonTypeInfoResolver` constructor and passes its resolver down. See [Trim / AOT-clean setup](aot-clean-setup.md).

## Limitations

- `MaxDepth` bounds the nesting when it is between 1 and 64. Omitting it — or writing the default `0` — leaves the nesting unbounded. A negative value, or one above 64, is the compile error `FN0021`: the first is meaningless and the second would expand into an unusable number of filter paths.
- The explicit `[MapNested<T>]` target must be a `[GenerateFilter<TNavigation>]` partial in the same compilation. A type that is not a filter class, one that targets a different entity, and one that lives in a referenced assembly all raise `FN0015 NestedFilterClassUnusable`.
- Collection navigations such as `User.Posts` are not supported and raise `FN0019 NestedCollectionUnsupported`.
- A `[MapNested]` whose named member does not exist, is a primitive or value type, or is not a single-target reference navigation raises `FN0018 NestedNavigationInvalid`.
- The same path produced by `[Map]`, `[PropertyMap]`, and `[MapNested]` together raises `FN0001 DuplicateMapping`, reporting every conflicting site.

## See also

- [Navigation paths and aliases](navigation-paths.md) for the per-column form.
- [Diagnostics catalogue](../diagnostics/index.md), rules `FN0014` to `FN0019`, `FN0021`, `FN0025`, and `FN0026`.
