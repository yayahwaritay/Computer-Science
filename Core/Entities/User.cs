using CompSci.Core.Enums;

namespace CompSci.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;

    /// <summary>
    /// Unique, human-readable identifier assigned to every Lecturer account at registration
    /// (e.g. "LEC-0001"), used to attribute activity to a specific lecturer. Null for non-Lecturer accounts.
    /// </summary>
    public string? LecturerId { get; set; }

    public bool IsApproved { get; set; } = true;
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
