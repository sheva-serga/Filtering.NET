using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

public sealed class MapNestedDbContext(DbContextOptions<MapNestedDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>().HasKey(company => company.Id);
        modelBuilder.Entity<Department>().HasKey(department => department.Id);
        modelBuilder.Entity<User>().HasKey(user => user.Id);
        // The seeds assign the keys; SQL Server rejects explicit values for an identity column.
        modelBuilder.Entity<Company>().Property(company => company.Id).ValueGeneratedNever();
        modelBuilder.Entity<Department>().Property(department => department.Id).ValueGeneratedNever();
        modelBuilder.Entity<User>().Property(user => user.Id).ValueGeneratedNever();
        modelBuilder.Entity<Employee>().Property(employee => employee.Id).ValueGeneratedNever();
        modelBuilder.Entity<Employee>().HasOne(employee => employee.Manager).WithMany().HasForeignKey(employee => employee.ManagerId);
    }
}
