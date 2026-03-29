namespace CompSci.Core.DTOs;

public class StudentRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int EnrollmentYear { get; set; }
    public int ExpectedGraduation { get; set; }
}

public class StudentResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int EnrollmentYear { get; set; }
    public int ExpectedGraduation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
