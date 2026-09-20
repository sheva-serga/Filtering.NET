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
    public UserFilter() : base(CreateSchema(serializerOptions: null)) { }

    internal static FilterSchema<User> CreateSchema(JsonSerializerOptions? serializerOptions) =>
        new FilterSchemaBuilder<User>(new FilterSettings(50, 200, 10, 50), serializerOptions)
            .Add(FilterProperty.Map("Name", (User entity) => entity.Name, StringFilter.Profile)
                .Sortable()
                .Build())
            .AddRange(DepartmentFilter.CreateSchema(serializerOptions)
                .LiftInto<User>(entity => entity.Department, "Department"))
            .Build();
}
```

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

`FilterGenerator.cs` registers two `ForAttributeWithMetadataName` pipelines:

1. **`[GenerateFilter<TEntity>]` branch.** Extracts a `FilterClassModel`, reports per-class diagnostics, and emits one source file per class. A collected view drives the assembly-wide `services.AddFiltering()` extension, the per-enum profiles, and `FilteringProfiles.g.cs`, which holds a runtime `FilterProfile<T>` instance for every custom profile your filters reference.
2. **`[FilterProfile<T>]` branch.** Extracts profile models and reports per-profile diagnostics such as `FN0009`, `FN0014`, `FN1001`, and `FN1007`.

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
