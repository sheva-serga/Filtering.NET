# UserManagement.WebApi sample

A minimal ASP.NET Core 9 Web API showing how to wire `Filtering.Net` end-to-end against an EF Core 9 + PostgreSQL backend. The sample is structured as a feature catalog &mdash; each `[Map]` / profile / interceptor block in `Filters/UserFilter.cs` is a tight, runnable demonstration of one library feature.

## What it demonstrates

| Feature | Where in the sample |
|---------|---------------------|
| `[GenerateFilter<T>]` partial class + DI registration | `Filters/UserFilter.cs`, `Program.cs` |
| Built-in primitive profiles (Int32, String, Bool, DateTime, Guid) | `Filters/UserFilter.cs` (Id, Age, IsActive, CreatedAt, ExternalId) |
| `[Map(Sortable = true, DefaultSortDirection = SortDir.Desc)]` | `MapAge`, `MapCreatedAt` |
| `[Map(Only = new[] { ... })]` operator allow-list | `MapEmail` (eq / contains / isNull only) |
| Custom `[FilterProfile<T>(BasedOn = ...)]` with custom `[FilterOperator]` | `Filters/StringFilterPlus.cs` &mdash; adds `fuzzy` and `ilike` to string columns |
| `EF.Functions.*` inside an operator expression | `Filters/StringFilterPlus.cs` &mdash; the `ilike` operator calls `EF.Functions.ILike` (Npgsql ILIKE) |
| Typed-value JSON deserialization + `JsonSerializerContext` wiring | `Json/SampleJsonContext.cs`, `Program.cs` `AddFiltering(SampleJsonContext.Default)` |
| `[InterceptValue]` pre-validation hook | `MapEmail` &mdash; lowercases the value via `NormalizeEmail` |
| Auto-emitted enum profile | `MapStatus` (the generator emits `Filtering.Net.Generated.UserStatusFilter` automatically) |
| Navigation path with friendly alias | `MapDepartmentName` &mdash; maps `Department.Name` as `departmentName` |
| Three controller endpoints | `Controllers/UsersController.cs` |

## Endpoints

- `POST /users/search` &mdash; validate + filter + page using `IQueryable<User>.ApplyPagedAsync(...)`.
- `POST /users/validate` &mdash; preview validation without hitting the database.
- `POST /users/export` &mdash; same filter shape but no paging cap (for "export-all" workflows).

## Prerequisites

- .NET 9 SDK
- Docker (optional &mdash; only for the bundled `docker-compose.yml` Postgres container)

## Run it

Start Postgres:

```sh
docker compose up -d
```

Run the API:

```sh
dotnet run
```

## Example requests

Substring match on Name (uses `contains` from the inherited built-in profile):

```sh
curl -X POST http://localhost:5000/users/search \
     -H "Content-Type: application/json" \
     -d '{
       "where": { "field": "Name", "operator": "contains", "value": "ali" },
       "sort":  [{ "field": "Name", "dir": 0 }],
       "page": 1,
       "pageSize": 10
     }'
```

Custom `fuzzy` operator from `StringFilterPlus` (case-insensitive substring):

```sh
curl -X POST http://localhost:5000/users/search \
     -H "Content-Type: application/json" \
     -d '{ "where": { "field": "Name", "operator": "fuzzy", "value": "ALI" } }'
```

Custom `ilike` operator from `StringFilterPlus` (SQL LIKE pattern via `EF.Functions.ILike` &mdash; Postgres-specific case-insensitive match). The caller supplies the LIKE pattern with `%` / `_` wildcards:

```sh
curl -X POST http://localhost:5000/users/search \
     -H "Content-Type: application/json" \
     -d '{ "where": { "field": "Name", "operator": "ilike", "value": "ali%" } }'
```

Aliased navigation path &mdash; targets the related `Department.Name` column under the friendly key `departmentName`:

```sh
curl -X POST http://localhost:5000/users/search \
     -H "Content-Type: application/json" \
     -d '{ "where": { "field": "departmentName", "operator": "eq", "value": "Engineering" } }'
```

Enum match on the auto-emitted `UserStatus` profile:

```sh
curl -X POST http://localhost:5000/users/search \
     -H "Content-Type: application/json" \
     -d '{ "where": { "field": "Status", "operator": "in", "value": ["Active", "Pending"] } }'
```

Operator restriction at work &mdash; this fails validation because `Email` is restricted to `eq` / `contains` / `isNull`:

```sh
curl -X POST http://localhost:5000/users/validate \
     -H "Content-Type: application/json" \
     -d '{ "where": { "field": "Email", "operator": "startsWith", "value": "alice" } }'
# 400 Bad Request — operator not allowed on Email
```

## Project layout

```
samples/UserManagement.WebApi/
├── Models/                      # User + Department EF entities (User carries a UserStatus enum)
├── Filters/
│   ├── UserFilter.cs            # [GenerateFilter<User>] partial — feature-by-feature catalogue
│   └── StringFilterPlus.cs      # custom [FilterProfile<string>] adding the fuzzy operator
├── Json/SampleJsonContext.cs    # JsonSerializerContext for trim/AOT-clean typed-value deserialization
├── Data/AppDbContext.cs         # EF Core context
├── Controllers/UsersController.cs
├── Program.cs                   # WebApplication setup + AddFiltering(SampleJsonContext.Default)
├── appsettings.json
└── docker-compose.yml           # Postgres container with named volume + healthcheck
```

## Adapting to other databases

Swap `UseNpgsql(...)` in `Program.cs` for `UseSqlServer`, `UseSqlite`, etc. The generated filter is provider-agnostic &mdash; only the `EF.Functions.*` translation surface changes per provider, and the source generator's FN1007 allow-list expands automatically when EF Core is referenced.

## Trim / AOT

The sample uses `AddFiltering(SampleJsonContext.Default)` rather than the parameterless overload so the typed-value deserialization in `MapName` (via the `fuzzy` operator) doesn't trigger IL2026 / IL3050 warnings under `PublishAot=true`. Filter classes whose properties only need element-extracted values (no custom operators with typed values, no `[PropertyMap]` overrides) don't emit the resolver-accepting constructor at all &mdash; the generator gates that emission per class.
