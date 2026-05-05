---
title: Testing your filter classes
description: Strategies for unit-testing against in-memory IQueryable and integration-testing against EF Core.
---

# Testing your filter classes

## What this does

Filter classes deserve two layers of test coverage:

1. **Unit tests against an in-memory `IQueryable<T>`** — instantiate the generated filter class directly, build a `FilterRequest`, call `query.Apply(filter, request).ToList()`, and assert the result. Fast, no I/O, doesn't exercise the EF translator but proves the predicate logic and the validation pipeline.
2. **Integration tests against a real EF provider** — typically SQLite in-memory (always available) and optionally Postgres / SQL Server via Testcontainers. Proves the predicate translates to SQL on the providers you ship against.

## When to use

Every filter class should carry at least one happy-path integration test plus a handful of validation-failure scenarios. Custom operators (especially provider-specific ones like `EF.Functions.ILike`) need provider-specific integration tests because the translation happens at SQL generation time, not at predicate compose time.

## Minimal code

Unit-test pattern with xUnit:

```csharp
public sealed class UserFilterTests
{
    [Fact]
    public void Apply_AgeGreaterThan_FiltersToOlderUsers()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = 1, Name = "Alice", Age = 25 },
            new User { Id = 2, Name = "Bob",   Age = 35 },
            new User { Id = 3, Name = "Carol", Age = 45 },
        }.AsQueryable();

        var filter = new UserFilter();
        var request = new FilterRequest
        {
            Where = new FilterGroup(LogicalOp.And, new FilterNode[]
            {
                new FilterLeaf("age", "gt", JsonDocument.Parse("30").RootElement),
            }),
        };

        // Act
        var matched = users.Apply(filter, request).ToList();

        // Assert
        matched.Select(user => user.Id).Should().BeEquivalentTo(new[] { 2, 3 });
    }
}
```

Integration-test pattern follows `tests/Filtering.Net.EntityFrameworkCore.Tests/Scenarios/`: SQLite in-memory connection plus a per-test `DbContext` fixture, seed via a small helper, then assert on `ApplyPagedAsync` output:

```csharp
[Fact]
public async Task ApplyPagedAsync_NameStartsWith_ReturnsMatchingPage()
{
    // Arrange
    using var fixture = await SqliteFixture.CreateAsync();
    await WidgetSeed.SeedAsync(fixture.DbContext);

    var request = new FilterRequest
    {
        Where = new FilterLeaf("name", "startsWith", JsonDocument.Parse("\"Wid\"").RootElement),
        Page = 1,
        PageSize = 10,
    };

    // Act
    var page = await fixture.DbContext.Widgets
        .ApplyPagedAsync(fixture.WidgetFilter, request);

    // Assert
    page.Items.Should().NotBeEmpty();
    page.TotalCount.Should().BeGreaterThan(0);
}
```

## Variations

- **Provider-specific scenarios** — wrap operators that depend on `EF.Functions.*` (e.g. `ILike` on Postgres) in a Testcontainers-backed fixture so the test runs against a real Postgres. The EF Core test project shows the pattern with `DockerAvailability.IsAvailable`–gated `Assert.Skip` blocks.
- **Validation-failure tests** — assert the negative path with `Assert.Throws<FilterValidationException>(() => users.Apply(filter, request).ToList())` and inspect `exception.Result.Errors` to confirm the right `FilterValidationCode` came through.
- **Snapshot tests for emitted code** — if you're a contributor changing the generator, the generator-test project under `tests/Filtering.Net.Generator.Tests/Emission/Snapshots/` already covers the emitted-source surface; consumer apps should not snapshot generated code.

## Pitfalls

- Generated code lives under `obj/` — don't import it directly in tests. Test only the public surface: `IFilterDefinition<T>` for typed access, plus the `Apply` / `ApplyPagedAsync` extension methods.
- Generated filter classes are named after the partial — `UserFilter`, not `Sample.UserFilter`. The namespace matches the partial's namespace.
- In-memory `IQueryable<T>` (LINQ-to-objects) translates predicates differently from EF. A test that passes against `users.AsQueryable()` does not prove the predicate translates to SQL — pair every meaningful operator with at least one integration test.
- The auto-emitted enum profiles (`Filtering.Net.Generated.<EnumName>Filter`) are emission targets you should *not* reference directly from test code; assert via the public filter class instead.

## See also

- [Async paged queries with `ApplyPagedAsync`](async-paged-queries.md)
- [Handling validation errors](handling-validation-errors.md)
