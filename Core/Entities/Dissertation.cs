namespace CompSci.Core.Entities;

public class Dissertation
{
    public Guid Id { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
