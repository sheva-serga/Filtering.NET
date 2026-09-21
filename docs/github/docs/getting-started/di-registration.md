---
title: DI registration
description: Wire up filters via the generated AddFiltering extension method.
---

# DI registration

## The generated AddFiltering extension

The generator emits an assembly-wide `services.AddFiltering()` extension method that registers every `[GenerateFilter<T>]` class in the assembly as a singleton `IFilterDefinition<T>`. You call it once, and every filter class becomes injectable by interface:

```csharp
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddFiltering(); // emitted by the generator
```

After this, controllers and handlers resolve `IFilterDefinition<User>` (or any other entity type) via constructor or `[FromServices]` injection. The generator only emits `AddFiltering()` when `Microsoft.Extensions.DependencyInjection.Abstractions` is referenced in the project.

If your project declares no `[GenerateFilter<T>]` partials, the extension is not emitted — there is nothing to register. The first declaration causes it to appear; subsequent declarations are folded into the same emission.

## Trim/AOT-clean variant

For trim or native-AOT scenarios where reflection-based JSON polymorphism is unavailable, the generator also emits an overload that accepts an `IJsonTypeInfoResolver`:

```csharp
builder.Services.AddFiltering(SampleJsonContext.Default);
```

`SampleJsonContext` is a `[JsonSerializable]`-annotated `JsonSerializerContext` declared by the consumer that lists every typed operator value in use. The sample app under `samples/UserManagement.WebApi/` includes a working `SampleJsonContext.Default` you can copy from.

The resolver is handed to each generated filter's constructor and ends up on the schema, where it is used to deserialize *typed operator values* — the arguments of value operators declared on your own `[FilterProfile<T>]` classes and of `[PropertyMap]` rules. Polymorphic `FilterNode` deserialization does not use it: `FilterNodeJsonConverter` reads a leaf's `value` as a raw `JsonElement` and never reflects over value types.

A third overload takes a `Func<IServiceProvider, IJsonTypeInfoResolver>` for cases where the resolver chain has to be composed from DI-resolved services.

See the AOT-clean setup [guide](../guides/aot-clean-setup.md) for the full pattern.

!!! note
    The resolver overload is only needed for filter classes that declare a typed-value operator or a `[PropertyMap]` rule — and for any filter that nests one. Classes whose properties only use built-in or auto-emitted enum profiles do not even get the resolver-accepting constructor, so the parameterless `AddFiltering()` is AOT-clean for them.

## See also

- [Your first request](your-first-request.md)
- [DI integration guide](../guides/di-integration.md)
- [AOT-clean setup](../guides/aot-clean-setup.md)
