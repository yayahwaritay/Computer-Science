using CompSci.Core.Enums;

namespace CompSci.Core.Entities;

/// <summary>
/// The internship placement record for one student in one academic year + semester: which host
/// Organization they're doing the internship with, and which Lecturer grades their internship
/// report (the 30-mark component of <see cref="InternshipEvaluation"/>). This is the record that
/// scopes both sides' access: an Organization only sees/evaluates students placed with it here,
/// and a Lecturer only grades reports for students allocated to it here. One row per student per
/// period (enforced by a unique index on StudentId + AcademicYear + Semester).
/// </summary>
public class InternshipAllocation
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    /// <summary>The host Organization's User.Id (User.Role == Organization) - the org this student is placed with.</summary>
    public Guid OrganizationUserId { get; set; }

    public Guid LecturerUserId { get; set; }
    public string AcademicYear { get; set; } = string.Empty; // e.g. "2021/2022"
    public Semester Semester { get; set; } = Semester.Second;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
