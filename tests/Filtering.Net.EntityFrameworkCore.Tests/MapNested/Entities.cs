namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

public sealed class Company
{
    public int Id { get; set; }
    public string Country { get; set; } = string.Empty;
}

public sealed class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Company Company { get; set; } = new();
}

public sealed class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
}

public sealed class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public Department Department { get; set; } = new();
}
