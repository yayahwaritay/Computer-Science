using CompSci.Core.Enums;

namespace CompSci.Core.DTOs;

public class InternshipAllocationRequest
{
    /// <summary>The student's human-readable Student ID (the Students.StudentId column, e.g. "24807") - not the internal database ID.</summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>The host Organization's own ID (Organization.Id, from GET /api/organizations) - the org this student is placed with.</summary>
    public Guid OrganizationId { get; set; }

    public Guid LecturerUserId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public Semester Semester { get; set; } = Semester.Second;
}

public class InternshipAllocationResponse
{
    public Guid Id { get; set; }

    /// <summary>The student's internal database ID (Students.Id) - see StudentIdNumber for the human-readable Student ID.</summary>
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentIdNumber { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;

    public Guid OrganizationId { get; set; }
    public Guid OrganizationUserId { get; set; }
    public string? OrganizationName { get; set; }

    public Guid LecturerUserId { get; set; }
    public string? LecturerUsername { get; set; }
    public string? LecturerId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public Semester Semester { get; set; }
    public string SemesterText { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Optional filter for listing allocations. All fields combine with AND.</summary>
public class InternshipAllocationFilter
{
    public string? AcademicYear { get; set; }
    public Semester? Semester { get; set; }
    public Guid? LecturerUserId { get; set; }

    /// <summary>Internal: the Organization's User.Id, used by GET /mine for an Organization caller.</summary>
    public Guid? OrganizationUserId { get; set; }

    /// <summary>The student's human-readable Student ID (e.g. "24807"), not the internal database ID.</summary>
    public string? StudentId { get; set; }
}
