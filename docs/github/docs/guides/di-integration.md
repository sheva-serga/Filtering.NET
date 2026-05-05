---
title: DI integration with AddFiltering
description: Register every generated filter as a singleton with one call.
---

# DI integration with `AddFiltering`

## What this does

When the consumer assembly references `Microsoft.Extensions.DependencyInjection.Abstractions`, the source generator emits an `IServiceCollection.AddFiltering()` extension method. That method registers every `[GenerateFilter<TEntity>]` class in the assembly as a singleton `IFilterDefinition<TEntity>`. Controllers and services then take an `IFilterDefinition<User>` (or any other entity) by constructor injection — no manual `services.AddSingleton<IFilterDefinition<User>, UserFilter>()` lines.

## When to use

Any DI-using app: ASP.NET Core, Worker Service, generic-host console apps. Filter classes are stateless after construction (the generator emits read-only metadata at compile time), so singleton lifetime is the right default.

## Minimal code

Lifted from `samples/UserManagement.WebApi/Program.cs`:

```csharp
using Filtering.Net;
using Microsoft.EntityFrameworkCore;
using UserManagement.WebApi.Data;
using UserManagement.WebApi.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDb")!));

// One call registers every [GenerateFilter<T>] partial as IFilterDefinition<T>.
builder.Services.AddFiltering();

var app = builder.Build();
app.MapControllers();
app.Run();
```

A controller injects the typed definition:

```csharp
public sealed class UsersController(IFilterDefinition<User> userFilter) : ControllerBase
{
    private readonly IFilterDefinition<User> _userFilter = userFilter;
    // ...
}
```

## Variations

- **`AddFiltering(IJsonTypeInfoResolver)`** — overload that accepts a `JsonSerializerContext` for AOT/trim-clean typed-value JSON deserialization. The sample app uses `builder.Services.AddFiltering(SampleJsonContext.Default);`. See [Trim / AOT-clean setup](aot-clean-setup.md).
- **Multiple consumer assemblies** — the generator emits one `AddFiltering` per assembly. If you split filter classes across assemblies, call `AddFiltering` once per assembly.
- **Manual registration** — if you need a non-singleton lifetime or a decorator, register `IFilterDefinition<T>` manually and skip `AddFiltering()` for that type.

## Pitfalls

- The `AddFiltering` extension is only emitted when the consumer assembly references `Microsoft.Extensions.DependencyInjection.Abstractions`. Without that reference, `services.AddFiltering()` won't compile and you'd register filters by hand. This is intentional — the generator avoids forcing a DI dependency on consumers that don't want one.
- The extension lives in the assembly's root namespace by default. If two consumer assemblies both define `AddFiltering` and you reference both, resolve the ambiguity by qualifying the call site (or moving filter classes into a single assembly).
- `AddFiltering` does not register `DbContext` or any data-access services for you. Wire those separately as the sample shows.

## See also

- [Trim / AOT-clean setup](aot-clean-setup.md)
- [DI registration in Getting started](../getting-started/di-registration.md)
