namespace UserManagement.WebApi.Models;

public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ExternalId { get; set; }
    public UserStatus Status { get; set; }

    public int DepartmentId { get; set; }
    // Non-nullable so the Department.Name dotted path mapped from UserFilter doesn't trigger FN1006.
    public Department Department { get; set; } = new();
}

public enum UserStatus
{
    Active,
    Pending,
    Suspended,
    Banned,
}
