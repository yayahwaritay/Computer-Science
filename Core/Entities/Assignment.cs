using CompSci.Core.Enums;

namespace CompSci.Core.Entities;

public class Assignment
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AssignmentTitle { get; set; } = string.Empty;
    public Importance Importance { get; set; } = Importance.Medium;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public string? FilePath { get; set; }
    public string? OriginalFileName { get; set; }
}
