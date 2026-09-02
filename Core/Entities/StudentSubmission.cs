using CompSci.Core.Enums;

namespace CompSci.Core.Entities;

/// <summary>
/// A student's own self-uploaded internship report or dissertation/project write-up. One row per
/// student per <see cref="SubmissionType"/> (enforced by a unique index on StudentId + Type) -
/// re-uploading overwrites the previous file in place (bumping <see cref="SubmissionCount"/> and
/// <see cref="SubmittedAt"/>) rather than creating a new row. Visible to the student who owns it,
/// plus Admin and whichever Lecturer is assigned to that student for this submission type (via
/// <see cref="InternshipAllocation"/> for InternshipReport, via <see cref="DissertationAllocation"/>
/// for Dissertation) - see StudentSubmissionService for the exact access rules.
/// </summary>
public class StudentSubmission
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public SubmissionType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>How many times the student has (re-)uploaded for this type. Starts at 1.</summary>
    public int SubmissionCount { get; set; } = 1;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
