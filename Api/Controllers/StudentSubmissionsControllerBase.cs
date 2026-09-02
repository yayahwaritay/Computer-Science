using System.Security.Claims;
using CompSci.Api.Filters;
using CompSci.Core.DTOs;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Shared implementation behind both InternshipReportsController and
/// DissertationSubmissionsController - the upload/view/download/comment behavior and access rules
/// are identical for both submission kinds (see IStudentSubmissionService), so only the
/// SubmissionType and a couple of display strings vary per concrete controller.
///
/// Access summary: a Student can upload/re-upload and view/download only their own submission and
/// its comments. Admin and the Lecturer assigned to that student (via InternshipAllocation for
/// InternshipReport, DissertationAllocation for Dissertation) can view/download the submission,
/// read its comments, and post new comments - which emails the owning student. Uploading emails
/// every Lecturer currently assigned to that student for this type.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Lecturer,Student")]
public abstract class StudentSubmissionsControllerBase : ControllerBase
{
    private readonly IStudentSubmissionService _submissionService;

    protected StudentSubmissionsControllerBase(IStudentSubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    protected abstract SubmissionType Type { get; }
    protected abstract string UploadedMessage { get; }
    protected abstract string NotFoundLabel { get; }

    /// <summary>
    /// Upload (or re-upload) the calling student's own submission - a re-upload overwrites the
    /// previous file, it does not create a second record (Student only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [Consumes("multipart/form-data")]
    [LogActivity("StudentSubmission", "Create")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<StudentSubmissionResponse>.FailResponse("Validation failed.", new List<string> { "File is required." }));

        var result = await _submissionService.UploadAsync(Type, CurrentUserId, file);
        return Ok(ApiResponse<StudentSubmissionResponse>.SuccessResponse(result, UploadedMessage));
    }

    /// <summary>Get the calling student's own submission (Student only)</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine()
    {
        var result = await _submissionService.GetMineAsync(Type, CurrentUserId);
        if (result == null)
            return NotFound(ApiResponse<StudentSubmissionResponse>.FailResponse($"You have not submitted your {NotFoundLabel} yet."));

        return Ok(ApiResponse<StudentSubmissionResponse>.SuccessResponse(result));
    }

    /// <summary>Get every submission (Admin) or only those assigned to the caller (Lecturer)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _submissionService.GetAllAsync(Type, AccessContext);
        return Ok(ApiResponse<IEnumerable<StudentSubmissionResponse>>.SuccessResponse(result));
    }

    /// <summary>Get a submission by ID (Admin/the assigned Lecturer only)</summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _submissionService.GetByIdAsync(Type, id, AccessContext);
        if (result == null)
            return NotFound(ApiResponse<StudentSubmissionResponse>.FailResponse($"Submission with ID {id} not found."));

        return Ok(ApiResponse<StudentSubmissionResponse>.SuccessResponse(result));
    }

    /// <summary>Download the submitted file (Admin/the assigned Lecturer/the owning Student only)</summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var (fileBytes, contentType, fileName) = await _submissionService.DownloadAsync(Type, id, AccessContext);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>Get every comment on a submission (Admin/the assigned Lecturer/the owning Student only)</summary>
    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(Guid id)
    {
        var result = await _submissionService.GetCommentsAsync(Type, id, AccessContext);
        return Ok(ApiResponse<IEnumerable<SubmissionCommentResponse>>.SuccessResponse(result));
    }

    /// <summary>Post a comment on a submission - emails the owning student (Admin/the assigned Lecturer only)</summary>
    [HttpPost("{id}/comments")]
    [Authorize(Roles = "Admin,Lecturer")]
    [LogActivity("SubmissionComment", "Create")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] SubmissionCommentRequest request)
    {
        var errors = SubmissionCommentValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<SubmissionCommentResponse>.FailResponse("Validation failed.", errors));

        var result = await _submissionService.AddCommentAsync(Type, id, CurrentUserId, request.Text, AccessContext);
        return Ok(ApiResponse<SubmissionCommentResponse>.SuccessResponse(result, "Comment added successfully."));
    }

    protected Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    protected SubmissionAccessContext AccessContext => new(CurrentUserId, User.IsInRole("Admin"));
}
