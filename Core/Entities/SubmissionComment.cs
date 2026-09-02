namespace CompSci.Core.Entities;

/// <summary>
/// A comment left by Admin/the assigned Lecturer on a student's <see cref="StudentSubmission"/>
/// (internship report or dissertation write-up). Only the student who owns the submission, plus
/// Admin/the assigned Lecturer, can read these - see StudentSubmissionService.
/// </summary>
public class SubmissionComment
{
    public Guid Id { get; set; }
    public Guid StudentSubmissionId { get; set; }
    public StudentSubmission? StudentSubmission { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
