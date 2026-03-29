using CompSci.Core.Enums;

namespace CompSci.Core.DTOs;

public class AssignmentRequest
{
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AssignmentTitle { get; set; } = string.Empty;
    public Importance Importance { get; set; } = Importance.Medium;
    public DateTime DueDate { get; set; }
}

public class AssignmentResponse
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AssignmentTitle { get; set; } = string.Empty;
    public Importance Importance { get; set; }
    public string ImportanceText { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DueDate { get; set; }
    public string? FilePath { get; set; }
    public string? OriginalFileName { get; set; }
}
