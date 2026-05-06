using Microsoft.EntityFrameworkCore;
using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Data;

public static class DatabaseInitializer
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var engineering = new Department { Name = "Engineering" };
        var marketing = new Department { Name = "Marketing" };
        var support = new Department { Name = "Support" };
        var finance = new Department { Name = "Finance" };

        dbContext.Departments.AddRange(engineering, marketing, support, finance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        dbContext.Users.AddRange(
            new User
            {
                Name = "Alice Anderson",
                Email = "alice.anderson@example.com",
                Age = 29,
                IsActive = true,
                CreatedAt = seedTimestamp.AddDays(1),
                ExternalId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Status = UserStatus.Active,
                Department = engineering,
            },
            new User
            {
                Name = "Bob Brown",
                Email = "bob.brown@example.com",
                Age = 41,
                IsActive = true,
                CreatedAt = seedTimestamp.AddDays(5),
                ExternalId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Status = UserStatus.Active,
                Department = engineering,
            },
            new User
            {
                Name = "Charlie Clark",
                Email = "charlie.clark@example.com",
                Age = 34,
                IsActive = false,
                CreatedAt = seedTimestamp.AddDays(10),
                ExternalId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Status = UserStatus.Suspended,
                Department = marketing,
            },
            new User
            {
                Name = "Diana Davis",
                Email = "diana.davis@example.com",
                Age = 27,
                IsActive = true,
                CreatedAt = seedTimestamp.AddDays(15),
                ExternalId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Status = UserStatus.Pending,
                Department = marketing,
            },
            new User
            {
                Name = "Ethan Evans",
                Email = "ethan.evans@example.com",
                Age = 52,
                IsActive = true,
                CreatedAt = seedTimestamp.AddDays(20),
                ExternalId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Status = UserStatus.Active,
                Department = support,
            },
            new User
            {
                Name = "Fiona Foster",
                Email = "fiona.foster@example.com",
                Age = 38,
                IsActive = false,
                CreatedAt = seedTimestamp.AddDays(25),
                ExternalId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Status = UserStatus.Banned,
                Department = support,
            },
            new User
            {
                Name = "George Garcia",
                Email = "george.garcia@example.com",
                Age = 23,
                IsActive = true,
                CreatedAt = seedTimestamp.AddDays(30),
                ExternalId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Status = UserStatus.Pending,
                Department = finance,
            },
            new User
            {
                Name = "Hannah Hill",
                Email = "hannah.hill@example.com",
                Age = 47,
                IsActive = true,
                CreatedAt = seedTimestamp.AddDays(35),
                ExternalId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Status = UserStatus.Active,
                Department = finance,
            },
            new User
            {
                Name = "Ivan Ivanov",
                Email = "ivan.ivanov@example.com",
                Age = 31,
                IsActive = true,
                CreatedAt = seedTimestamp.AddDays(40),
                ExternalId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Status = UserStatus.Active,
                Department = engineering,
            },
            new User
            {
                Name = "Julia Jones",
                Email = "julia.jones@example.com",
                Age = 26,
                IsActive = false,
                CreatedAt = seedTimestamp.AddDays(45),
                ExternalId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Status = UserStatus.Suspended,
                Department = marketing,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
