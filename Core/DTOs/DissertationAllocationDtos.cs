namespace CompSci.Core.DTOs;

public class DissertationAllocationRequest
{
    /// <summary>The student's human-readable Student ID (the Students.StudentId column, e.g. "24807") - not the internal database ID.</summary>
    public string StudentId { get; set; } = string.Empty;

    public Guid LecturerUserId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
}

public class DissertationAllocationResponse
{
    public Guid Id { get; set; }

    /// <summary>The student's internal database ID (Students.Id) - see StudentIdNumber for the human-readable Student ID.</summary>
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentIdNumber { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;

    public Guid LecturerUserId { get; set; }
    public string? LecturerUsername { get; set; }
    public string? LecturerId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Optional filter for listing dissertation supervision allocations. All fields combine with AND.</summary>
public class DissertationAllocationFilter
{
    public string? AcademicYear { get; set; }
    public Guid? LecturerUserId { get; set; }

    /// <summary>The student's human-readable Student ID (e.g. "24807"), not the internal database ID.</summary>
    public string? StudentId { get; set; }
}
