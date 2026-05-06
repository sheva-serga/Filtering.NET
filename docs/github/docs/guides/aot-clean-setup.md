---
title: Trim / AOT-clean setup
description: Pair AddFiltering with a JsonSerializerContext to silence IL2026/IL3050 under PublishAot.
---

# Trim / AOT-clean setup

## What this does

Typed-value JSON deserialization — used when a custom `[FilterOperator]` returns `Expression<Func<TColumn, TValue, bool>>` where `TValue` is a non-primitive type, or when a `[PropertyMap]` override declares a typed value — calls into `JsonSerializer`'s reflection-based APIs by default. Those APIs are incompatible with `PublishAot` and aggressive trimming, surfacing as `IL2026` / `IL3050` warnings at build time. Passing a `JsonSerializerContext` to `AddFiltering(IJsonTypeInfoResolver)` switches deserialization to source-generated converters, eliminating the reflection.

## When to use

- **Native AOT–published apps** (`<PublishAot>true</PublishAot>`).
- **Trimmed apps** (`<PublishTrimmed>true</PublishTrimmed>`).
- **Anywhere `IL2026` / `IL3050` warnings appear** in the build output, even if you aren't publishing AOT yet — the same warnings become errors when AOT is turned on.

## Minimal code

Lifted from `samples/UserManagement.WebApi/Json/SampleJsonContext.cs` and `Program.cs`:

```csharp
// Json/SampleJsonContext.cs
using System.Text.Json.Serialization;

namespace UserManagement.WebApi.Json;

// Trim/AOT-safe resolver. Add a [JsonSerializable] entry per typed-value type
// used by custom operators or [PropertyMap] overrides.
[JsonSerializable(typeof(string))]
public partial class SampleJsonContext : JsonSerializerContext;
```

```csharp
// Program.cs
using Filtering.Net;
using UserManagement.WebApi.Json;

var builder = WebApplication.CreateBuilder(args);

// ... DbContext registration ...

// Resolver overload: routes typed-value deserialization through SampleJsonContext.
builder.Services.AddFiltering(SampleJsonContext.Default);

var app = builder.Build();
app.MapControllers();
app.Run();
```

## Variations

- **Multiple `[JsonSerializable]` lines** — declare one per typed value used by your custom operators. A custom operator with `Expression<Func<DateTime, MyDateRange, bool>>` requires `[JsonSerializable(typeof(MyDateRange))]` on the context.
- **Reuse an existing app-level context** — if your app already has a `JsonSerializerContext` for ASP.NET Core JSON serialization, you can pass that same instance to `AddFiltering`. Just ensure every typed value used by filter operators is registered on it.
- **Combine multiple contexts** — use `JsonTypeInfoResolver.Combine(...)` to merge an app context and a filter-specific context if you prefer keeping them separate.

## Pitfalls

- `FN1008` warns when a typed value isn't registered in any visible `JsonSerializerContext`. The diagnostic is **opt-in**: add `[assembly: FilterValueDiagnostics(WarnUnregistered = true)]` to your project to enable it. Without that opt-in, the generator stays quiet because reflection-fallback is the documented happy path for non-AOT consumers.
- Filter classes whose properties only need element-extracted values from built-in profiles (no custom-operator typed values, no `[PropertyMap]` overrides) **don't emit the resolver-accepting constructor** at all — the generator gates that emission per class. For those classes the `IJsonTypeInfoResolver` is a no-op and you can omit the `AddFiltering` overload entirely.
- A `JsonSerializerContext` is a compile-time construct. Adding a typed value to a custom operator after the context is generated requires updating the context's `[JsonSerializable]` lines — the context cannot pick up new types at runtime.

## See also

- [DI integration with AddFiltering](di-integration.md)
