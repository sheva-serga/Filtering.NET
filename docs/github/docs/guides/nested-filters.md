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

- `[MapNested(nameof(User.Department))]` resolves the unique `[GenerateFilter<Department>]` partial in the compilation. Two candidates raise `FN0017`.
- `[MapNested<DepartmentFilter>(nameof(User.Department))]` names the filter class. Use it when several filter classes target the same entity.

## Configuration knobs

- `Prefix = "dept"` changes the prefix on the wire. The CLR path `Department.Name` stays valid as a key too.
- `Only = new[] { "Id", "Name" }` keeps only these paths. Paths are relative to the nested filter, in CLR PascalCase.
- `Except = new[] { "InternalNotes" }` drops these paths.
- `DisableSorting = true` makes every property that arrives through this nesting non-sortable.
- `MaxDepth = 2` bounds how often this nesting is followed along one path. See the next section.

## Transitive nesting

If `DepartmentFilter` itself has `[MapNested(nameof(Department.Company))]`, then `UserFilter` exposes `department.company.*` as well. `Only` and `Except` apply to the nested filter's own mappings. Properties it nests in turn follow their own `[MapNested]` settings.

## Self-referencing and circular models

Entity models are often circular: `Employee.Manager` points at another `Employee`, or `User.Department` and `Department.Head` point at each other. Nesting such a graph without a limit would never end, so an unbounded cycle is the compile error `FN0015 NestedCycle`. Give at least one nesting in the cycle a `MaxDepth` and the cycle becomes legal:

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

## Typed operator values

If the nested filter, or anything it nests, has an operator that takes a typed value, the host also gets the `IJsonTypeInfoResolver` constructor and passes its resolver down. See [Trim / AOT-clean setup](aot-clean-setup.md).

## Limitations

- A negative `MaxDepth` is the compile error `FN0022`.
- The target filter class must live in the same compilation. Otherwise `FN0016 NestedCrossAssembly` fires.
- Collection navigations such as `User.Posts` are not supported and raise `FN0020 NestedCollectionUnsupported`.
- The same path produced by `[Map]`, `[PropertyMap]`, and `[MapNested]` together raises `FN0001 DuplicateMapping`, reporting every conflicting site.

## See also

- [Navigation paths and aliases](navigation-paths.md) for the per-column form.
- [Diagnostics catalogue](../diagnostics/index.md), rules `FN0015` to `FN0020`, and `FN0022`.
