namespace CompSci.Core.Entities;

/// <summary>
/// Profile for a host organization/company account (User.Role == Organization), mirroring how
/// <see cref="Student"/> extends a User. Registered by Admin/Lecturer from the email the
/// organization sent in; used only to evaluate the students it hosted for internship.
/// </summary>
public class Organization
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
