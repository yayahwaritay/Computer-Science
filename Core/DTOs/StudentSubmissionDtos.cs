using CompSci.Core.Enums;

namespace CompSci.Core.DTOs;

public class StudentSubmissionResponse
{
    public Guid Id { get; set; }

    /// <summary>The student's internal database ID (Students.Id) - see StudentIdNumber for the human-readable Student ID.</summary>
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentIdNumber { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;

    public SubmissionType Type { get; set; }
    public string TypeText { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public int SubmissionCount { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CommentCount { get; set; }
}

public class SubmissionCommentRequest
{
    public string Text { get; set; } = string.Empty;
}

public class SubmissionCommentResponse
{
    public Guid Id { get; set; }
    public Guid StudentSubmissionId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Identifies the caller for StudentSubmission/SubmissionComment access control. Admin can see/manage
/// every submission of the given type; anyone else's access is resolved dynamically by
/// StudentSubmissionService against both "is this the owning student" and "is this the Lecturer
/// assigned to this student for this submission type" - so the same context works whether the
/// caller reached the endpoint via a Student, Lecturer, or Admin role.
/// </summary>
public record SubmissionAccessContext(Guid CallerUserId, bool IsAdmin);
