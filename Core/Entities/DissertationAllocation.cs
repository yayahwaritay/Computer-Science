namespace CompSci.Core.Entities;

/// <summary>
/// Assigns the supervising Lecturer for one student's dissertation/final-year-project write-up, for
/// a given academic year. This is what scopes access to that student's dissertation submission (see
/// <see cref="StudentSubmission"/>) - only Admin and the assigned Lecturer can see/comment on it.
/// One row per student per academic year (enforced by a unique index on StudentId + AcademicYear).
/// Mirrors <see cref="InternshipAllocation"/>'s role for internship report submissions.
/// </summary>
public class DissertationAllocation
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid LecturerUserId { get; set; }
    public string AcademicYear { get; set; } = string.Empty; // e.g. "2025/2026"
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
