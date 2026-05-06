using Filtering.Net;
using UserManagement.WebApi.Data;
using UserManagement.WebApi.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(jsonOptions =>
{
    jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddDbContext<AppDbContext>(dbOptions =>
    dbOptions.UseNpgsql(builder.Configuration.GetConnectionString("AppDb")
        ?? "Host=localhost;Port=5432;Database=user_management;Username=postgres;Password=postgres"));

// Resolver overload: routes typed-value deserialization through SampleJsonContext for trim/AOT safety.
builder.Services.AddFiltering(SampleJsonContext.Default);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

await DatabaseInitializer.MigrateAndSeedAsync(app.Services);

app.MapControllers();
app.MapGet("/", () =>
    "Filtering.Net sample. POST a FilterRequest to /users/search, /users/validate, or /users/export.");

app.Run();
