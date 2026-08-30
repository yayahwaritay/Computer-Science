using CompSci.Core.Enums;

namespace CompSci.Core.Entities;

/// <summary>
/// One row of an official course allocation table: a course, in a given program/year of study,
/// for a given academic year + semester, allocated to a member of staff. A full document (like the
/// "2021_2022 Second Semester Allocation") is simply every row sharing the same AcademicYear +
/// Semester, grouped by ProgramName then YearOfStudy for display/export.
/// </summary>
public class CourseAllocation
{
    public Guid Id { get; set; }
    public string AcademicYear { get; set; } = string.Empty; // e.g. "2021/2022"
    public Semester Semester { get; set; } = Semester.Second;
    public string ProgramName { get; set; } = string.Empty; // e.g. "B.Sc. (Hons) Computer Science"
    public int YearOfStudy { get; set; } // 1 = First Year, 2 = Second Year, ...
    public string CourseCode { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public string CreditHours { get; set; } = string.Empty; // e.g. "3" or "3(P)" (P = practical)
    public string StaffName { get; set; } = string.Empty; // display name, e.g. "Y. L. Kamara" or "Engl. Dept"

    /// <summary>
    /// Optional link to the Lecturer's own login account, so they can pull up "my allocation" —
    /// left null for staff without a Lecturer account (e.g. "Engl. Dept").
    /// </summary>
    public Guid? LecturerUserId { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
