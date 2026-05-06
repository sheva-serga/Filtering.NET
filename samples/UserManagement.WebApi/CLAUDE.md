# CLAUDE.md — UserManagement.WebApi sample

A minimal ASP.NET Core 9 + EF Core 9 + PostgreSQL Web API that exercises `Filtering.Net` end-to-end. Treat this as the canonical "how would I wire this in production" reference, and as a feature catalogue — each grouping in `Filters/UserFilter.cs` demonstrates one feature with a comment explaining its purpose.

**Target:** `net9.0`. References `Filtering.Net`, `Filtering.Net.Generator` (analyzer), `Filtering.Net.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`.

## What it demonstrates

- **`[GenerateFilter<User>]` partial** — `Filters/UserFilter.cs`. Each `[Map]` group inside is a feature demo (primitives, custom-profile, operator restriction, interceptor, enum, navigation alias).
- **Custom profile with typed-value operator** — `Filters/StringFilterPlus.cs`. `[FilterProfile<string>(BasedOn = typeof(StringFilter))]` + two custom operators: `[FilterOperator("fuzzy")]` (substring) and `[FilterOperator("ilike")]` (calls `EF.Functions.ILike` directly — the canonical example of a `[FilterOperator]` lambda invoking a provider-specific EF function). Adding any `string`-typed custom profile makes `string` an ambiguous match for built-in resolution, so every string-typed `[Map]` in `UserFilter` specifies `Profile = typeof(...)` explicitly (this is the FN0014 contract).
- **`[InterceptValue]`** — `NormalizeEmail` lowercases the email value before predicate building. Interceptors must be `internal` or `public` (the generator's per-property class is `file`-scoped, separate compilation unit).
- **Auto-emitted enum profile** — `User.Status` is a `UserStatus` enum; the generator scans the property graph and emits `Filtering.Net.Generated.UserStatusFilter` automatically.
- **Navigation path + alias** — `[Map("Department.Name", Alias = "departmentName")]` exposes the related column under a friendly key.
- **`AddFiltering(IJsonTypeInfoResolver)` overload** — `Program.cs` passes `SampleJsonContext.Default` so typed-value deserialization is trim/AOT-clean.
- **Three controller endpoints** in `Controllers/UsersController.cs`:
  - `POST /users/search` — validate + filter + page via `IQueryable<User>.ApplyPagedAsync(...)`.
  - `POST /users/validate` — preview validation without hitting the database.
  - `POST /users/export` — same filter shape, no paging cap.

## Folder structure

```
samples/UserManagement.WebApi/
├── Models/                      # User + Department EF entities (User carries a UserStatus enum)
├── Filters/
│   ├── UserFilter.cs            # [GenerateFilter<User>] partial — feature catalogue
│   └── StringFilterPlus.cs      # custom [FilterProfile<string>] adding the fuzzy operator
├── Json/SampleJsonContext.cs    # JsonSerializerContext for trim/AOT-clean typed-value deserialization
├── Data/
│   ├── AppDbContext.cs          # EF Core context
│   └── DatabaseInitializer.cs   # MigrateAsync + idempotent demo-data seeder (4 departments + 10 users)
├── Migrations/                  # EF Core migrations (InitialCreate is the only one)
├── Controllers/UsersController.cs
├── Program.cs                   # WebApplication setup + AddFiltering(SampleJsonContext.Default) + DatabaseInitializer.MigrateAndSeedAsync
├── appsettings.json
├── docker-compose.yml           # Postgres container with healthcheck
└── README.md
```

## Editing rules

- **Each feature demo stays small.** Ideally a single grouping inside `UserFilter.cs` with a leading comment naming the feature. If a demo grows past ~20 lines or pulls in new files, move it to a separate sample.
- **Keep the entity model tiny.** `User` + `Department` is enough. Adding more entities just adds noise to the catalogue.
- **Mirror the README.** Any new endpoint, filter feature, or DI registration that lands here should also appear in the README walkthrough table.

## Running locally

```sh
docker compose up -d   # Postgres on :5432
dotnet run             # API on :5000 / :5001 — calls DatabaseInitializer.MigrateAndSeedAsync at startup
```

`Program.cs` awaits `DatabaseInitializer.MigrateAndSeedAsync(app.Services)` before `app.MapControllers()`, so a fresh container gets the schema (via `Database.MigrateAsync`) and the demo seed in one shot. `MigrateAsync` is idempotent; the seeder short-circuits when `Users` is non-empty. Apply migrations explicitly with `dotnet ef database update --project samples/UserManagement.WebApi` if you want to inspect the SQL before launch.

The connection string in `appsettings.json` matches the docker-compose defaults. Swap `UseNpgsql` in `Program.cs` for `UseSqlServer` / `UseSqlite` to retarget — `Filtering.Net` itself is provider-agnostic. Note that the `Migrations/` folder is Npgsql-specific (it carries `Npgsql:ValueGenerationStrategy` annotations); switching providers means re-generating it.

## What's intentionally omitted

- Auth, logging, OpenAPI generation — orthogonal to filtering and would only obscure the example.
- Multi-tenant filtering — handled at the `IQueryable<User>` source (e.g., `dbContext.Users.Where(u => u.TenantId == tenantId)`) before passing to `ApplyPagedAsync`. Outside this sample's scope.
- `[PropertyMap]` full DSL override — supported by the library but requires enough setup that it belongs in a dedicated sample.
