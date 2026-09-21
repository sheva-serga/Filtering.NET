---
title: How it works
description: What the generator emits, what the runtime engine does, and where the line between them sits.
---

# How it works

## Generated schema, generic engine

Filtering.Net splits the work in two:

- **The source generator** reads each `[GenerateFilter<TEntity>]` partial and emits a small *schema*: one entry per mapping, holding a typed accessor lambda (`entity => entity.Department.Name`), a reference to a profile, and the options you set (`Alias`, `Sortable`, `Only`, interceptors).
- **The runtime engine**, `FilterDefinition<TEntity>`, is ordinary library code shared by every filter. It validates requests against the schema, composes predicates, and applies sorting and paging.

The generated half of a filter class is about thirty lines. It declares `FilterDefinition<TEntity>` as the base class and implements one method:

```csharp
partial class UserFilter : FilterDefinition<User>
{
    public UserFilter() : base(CreateSchema(serializerOptions: null, FilterNestingContext.Root)) { }

    internal static FilterSchema<User> CreateSchema(JsonSerializerOptions? serializerOptions, FilterNestingContext nestingContext) =>
        new FilterSchemaBuilder<User>(new FilterSettings(50, 200, 10, 50), serializerOptions)
            .Add(FilterProperty.Map("Name", (User entity) => entity.Name, StringFilter.Profile)
                .Sortable()
                .Build())
            .AddNested(nestingContext, "Sample.UserFilter.Department", maxDepth: 0,
                nestedContext => DepartmentFilter.CreateSchema(serializerOptions, nestedContext),
                (User entity) => entity.Department, "Department", only: null, except: null, disableSorting: false)
            .Build();
}
```

The second argument to `AddNested` is the *nesting key*: `<filter class fully-qualified name>.<navigation property>`, plus `@<prefix>` when `Prefix` differs from the navigation name. `FilterNestingContext` counts entries per key, which is what bounds `MaxDepth` in a circular filter graph.

## How a predicate is built

Profiles hold typed operator templates such as `(column, value) => column.Contains(value)`. When a request arrives, the engine takes the operator template, substitutes the property's accessor for `column`, substitutes the parsed value for `value`, and hands the result to `IQueryable.Where`. The substitution is an `ExpressionVisitor` pass over trees the C# compiler already built and type-checked.

What this means in practice:

- **No reflection over your types.** Accessors and operators are compiler-checked lambdas. Nothing is looked up by name at runtime.
- **No `Compile()`.** The engine only rearranges expression trees; your query provider consumes them.
- **Trim and Native AOT clean.** See [Trim / AOT-clean setup](../guides/aot-clean-setup.md).
- **Values become SQL parameters.** A value is spliced in as a member access on a holder object, the same shape a C# closure produces, so EF Core parameterizes it instead of inlining a literal.
- **Nullable columns behave like C#.** For an `int?` property mapped to `Int32Filter`, comparisons are lifted exactly as the compiler lifts `entity.Score == value`.

Predicates are composed per request. The per-property work that does not depend on the request (splicing accessors into operators, building sort delegates, unary predicates such as `isNull`) happens once, when the filter is constructed.

## The generator pipeline

`FilterGenerator.cs` registers one compilation-wide index node plus two `ForAttributeWithMetadataName` pipelines:

0. **The generator index.** One pass over the compilation collects the `[FilterProfile<T>]` types and their operators, the enums that need an auto-emitted profile, the assembly-level `[FilterDefaults]`, and whether the DI abstractions are referenced. Every other node combines with it, so nothing downstream has to read compilation-global state from a syntax transform.
1. **`[GenerateFilter<TEntity>]` branch.** A syntax transform reads the class's own tree into a declaration, a resolver combines that with the index into a `FilterClassModel`, and a nesting pass splices `[MapNested]` targets together. The emitter writes one source file per class. A collected view drives the assembly-wide `services.AddFiltering()` extension, the per-enum profiles, and `FilteringProfiles.g.cs`, which holds a runtime `FilterProfile<T>` instance for every custom profile your filters reference.
2. **`[FilterProfile<T>]` branch.** Extracts profile models and reports per-profile diagnostics such as `FN0008`, `FN0010`, `FN0013`, `FN0028`, `FN1001`, and `FN1007`.

Cross-pipeline diagnostics, `FN1003 ProfileUnused` and `FN1004 OperatorUnused`, join both outputs.

Your own code runs as written. A `[FilterOperator]` member is referenced directly from the generated profile instance, and a `[PropertyMap]` method is called once when the filter is constructed. The analyzer still inspects those lambdas for diagnostics, but nothing is copied or rewritten.

## What the consumer sees

- Declare a `[GenerateFilter<TEntity>]` partial. The generator emits the schema into `obj/`.
- The generated `services.AddFiltering()` registers every filter class as a singleton `IFilterDefinition<T>`.
- A controller resolves `IFilterDefinition<T>` and calls `Apply(...)` or the async EF helper `ApplyPagedAsync(...)`.
- `Apply` runs `Validate(request)` first and throws `FilterValidationException` with a structured result on failure.
- On success it calls `ApplyFilter` and then `ApplySorting`.

Configuration mistakes that the analyzer cannot see, for example a duplicate wire key in a hand-built schema, throw `FilterConfigurationException` when the filter is constructed. With the singleton registration that is the first resolve, not a request in production traffic.

## See also

- [Profiles and operators](profiles-and-operators.md)
- [Validation philosophy](validation-philosophy.md)
- [Building a definition by hand](../guides/hand-built-definitions.md)
