using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

public sealed class MapNestedDbContext(DbContextOptions<MapNestedDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>().HasKey(company => company.Id);
        modelBuilder.Entity<Department>().HasKey(department => department.Id);
        modelBuilder.Entity<User>().HasKey(user => user.Id);
    }
}
