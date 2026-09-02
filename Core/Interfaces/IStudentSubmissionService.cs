using CompSci.Core.DTOs;
using CompSci.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Interfaces;

/// <summary>
/// Backs both the internship report and dissertation write-up self-submission endpoints - every
/// method is parametrized by <see cref="SubmissionType"/> rather than there being two near-identical
/// service classes, since the upload/comment/visibility/notification behavior is identical for both;
/// only how "the assigned Lecturer" is resolved differs (InternshipAllocation vs DissertationAllocation).
/// </summary>
public interface IStudentSubmissionService
{
    /// <summary>
    /// Uploads (or re-uploads, overwriting the previous file) the calling student's own submission,
    /// and emails every Lecturer currently assigned to that student for this type, if any.
    /// </summary>
    Task<StudentSubmissionResponse> UploadAsync(SubmissionType type, Guid studentUserId, IFormFile file);

    /// <summary>The calling student's own submission, or null if they haven't uploaded one yet.</summary>
    Task<StudentSubmissionResponse?> GetMineAsync(SubmissionType type, Guid studentUserId);

    /// <summary>Admin: every submission of this type. Non-Admin (Lecturer): only students assigned to them.</summary>
    Task<IEnumerable<StudentSubmissionResponse>> GetAllAsync(SubmissionType type, SubmissionAccessContext access);

    /// <summary>Admin/assigned Lecturer/owning student only - anyone else gets a 404-style null.</summary>
    Task<StudentSubmissionResponse?> GetByIdAsync(SubmissionType type, Guid id, SubmissionAccessContext access);

    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(SubmissionType type, Guid id, SubmissionAccessContext access);

    Task<IEnumerable<SubmissionCommentResponse>> GetCommentsAsync(SubmissionType type, Guid submissionId, SubmissionAccessContext access);

    /// <summary>Admin/assigned Lecturer only. Emails the owning student that a new comment was posted.</summary>
    Task<SubmissionCommentResponse> AddCommentAsync(SubmissionType type, Guid submissionId, Guid authorUserId, string text, SubmissionAccessContext access);
}
