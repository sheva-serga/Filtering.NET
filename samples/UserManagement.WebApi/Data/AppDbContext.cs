using Microsoft.EntityFrameworkCore;
using UserManagement.WebApi.Models;

namespace UserManagement.WebApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(userEntity =>
        {
            userEntity.HasKey(user => user.Id);
            userEntity.HasIndex(user => user.Email).IsUnique();
            userEntity.HasOne(user => user.Department)
                .WithMany()
                .HasForeignKey(user => user.DepartmentId);
        });
        modelBuilder.Entity<Department>(departmentEntity =>
        {
            departmentEntity.HasKey(department => department.Id);
        });
    }
}
