using CompSci.Core.Enums;

namespace CompSci.Core.DTOs;

public class CourseAllocationRequest
{
    public string AcademicYear { get; set; } = string.Empty;
    public Semester Semester { get; set; } = Semester.Second;
    public string ProgramName { get; set; } = string.Empty;
    public int YearOfStudy { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public string CreditHours { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public Guid? LecturerUserId { get; set; }
}

/// <summary>
/// Lets Admin allocate a whole table in one call (e.g. every row of one program's listing, or the
/// entire semester across every program) instead of one HTTP request per course row — this is what
/// keeps allocation simple for Admin.
/// </summary>
public class CourseAllocationBulkRequest
{
    public List<CourseAllocationRequest> Allocations { get; set; } = new();
}

public class CourseAllocationResponse
{
    public Guid Id { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public Semester Semester { get; set; }
    public string SemesterText { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int YearOfStudy { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public string CreditHours { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public Guid? LecturerUserId { get; set; }
    public string? LecturerUsername { get; set; }
    public string? LecturerId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Optional filter used for listing/exporting allocations. AcademicYear + Semester identify one
/// compiled document (e.g. "2021/2022" + Second = "2021_2022 Second Semester Allocation");
/// ProgramName narrows to a single program's table; LecturerUserId narrows to one lecturer's rows
/// (used by the "my allocation" endpoints).
/// </summary>
public class CourseAllocationFilter
{
    public string? AcademicYear { get; set; }
    public Semester? Semester { get; set; }
    public string? ProgramName { get; set; }
    public Guid? LecturerUserId { get; set; }
}
