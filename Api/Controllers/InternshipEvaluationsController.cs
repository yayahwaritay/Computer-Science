using System.Security.Claims;
using CompSci.Api.Filters;
using CompSci.Core.DTOs;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Digital "Student Internship Evaluation Form". Organizations submit the 14-criteria evaluation
/// (scaled to a 70-point EvaluationScore); the lecturer allocated to the student then grades the
/// internship report out of 30 (ReportScore), producing a 0-100 TotalScore and A-F Grade.
/// Organization is scoped to what it submitted, Lecturer to what's allocated to it, Admin sees all.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lecturer,Organization")]
public class InternshipEvaluationsController : ControllerBase
{
    private readonly IInternshipEvaluationService _internshipEvaluationService;

    public InternshipEvaluationsController(IInternshipEvaluationService internshipEvaluationService)
    {
        _internshipEvaluationService = internshipEvaluationService;
    }

    /// <summary>
    /// Submit an internship evaluation for a student (Organization only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Organization")]
    [LogActivity("InternshipEvaluation", "Create")]
    public async Task<IActionResult> Create([FromBody] InternshipEvaluationRequest request)
    {
        var errors = InternshipEvaluationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<InternshipEvaluationResponse>.FailResponse("Validation failed.", errors));

        var result = await _internshipEvaluationService.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<InternshipEvaluationResponse>.SuccessResponse(result, "Internship evaluation submitted successfully."));
    }

    /// <summary>
    /// Get all evaluations visible to the caller (Admin: all, Lecturer: allocated to them, Organization: submitted by them)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _internshipEvaluationService.GetAllAsync(AccessContext);
        return Ok(ApiResponse<IEnumerable<InternshipEvaluationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get evaluations for a given student (scoped as per GetAll)
    /// </summary>
    [HttpGet("by-student")]
    public async Task<IActionResult> GetByStudent([FromQuery] string studentId)
    {
        var result = await _internshipEvaluationService.GetByStudentIdAsync(studentId, AccessContext);
        return Ok(ApiResponse<IEnumerable<InternshipEvaluationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get the compiled per-program grade report (Name, ID, Evaluation Score, Report Score, Grade) (Admin/Lecturer only)
    /// </summary>
    [HttpGet("compiled")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> GetCompiled(
        [FromQuery] string? programName,
        [FromQuery] string? academicYear,
        [FromQuery] Semester? semester)
    {
        var result = await _internshipEvaluationService.GetCompiledAsync(new InternshipEvaluationFilter
        {
            ProgramName = programName,
            AcademicYear = academicYear,
            Semester = semester
        });

        return Ok(ApiResponse<IEnumerable<CompiledGradeReport>>.SuccessResponse(result));
    }

    /// <summary>
    /// Download the compiled per-program grade report as CSV (Admin/Lecturer only)
    /// </summary>
    [HttpGet("compiled/export/csv")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> ExportCompiledCsv(
        [FromQuery] string? programName,
        [FromQuery] string? academicYear,
        [FromQuery] Semester? semester)
    {
        var bytes = await _internshipEvaluationService.ExportCompiledCsvAsync(new InternshipEvaluationFilter
        {
            ProgramName = programName,
            AcademicYear = academicYear,
            Semester = semester
        });

        return File(bytes, "text/csv", $"internship_grades_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    /// <summary>
    /// Download the compiled per-program grade report as PDF (Admin/Lecturer only)
    /// </summary>
    [HttpGet("compiled/export/pdf")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> ExportCompiledPdf(
        [FromQuery] string? programName,
        [FromQuery] string? academicYear,
        [FromQuery] Semester? semester)
    {
        var bytes = await _internshipEvaluationService.ExportCompiledPdfAsync(new InternshipEvaluationFilter
        {
            ProgramName = programName,
            AcademicYear = academicYear,
            Semester = semester
        });

        return File(bytes, "application/pdf", $"internship_grades_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    /// <summary>
    /// Get an evaluation by ID (scoped as per GetAll)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _internshipEvaluationService.GetByIdAsync(id, AccessContext);
        if (result == null)
            return NotFound(ApiResponse<InternshipEvaluationResponse>.FailResponse($"Internship evaluation with ID {id} not found."));

        return Ok(ApiResponse<InternshipEvaluationResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Grade the internship report out of 30 (the lecturer allocated to this student, or Admin)
    /// </summary>
    [HttpPut("{id}/report-grade")]
    [Authorize(Roles = "Admin,Lecturer")]
    [LogActivity("InternshipEvaluation", "Update")]
    public async Task<IActionResult> SubmitReportGrade(Guid id, [FromBody] ReportGradeRequest request)
    {
        var errors = InternshipEvaluationValidator.ValidateReportGrade(request);
        if (errors.Any())
            return BadRequest(ApiResponse<InternshipEvaluationResponse>.FailResponse("Validation failed.", errors));

        var result = await _internshipEvaluationService.SubmitReportGradeAsync(id, request, AccessContext);
        return Ok(ApiResponse<InternshipEvaluationResponse>.SuccessResponse(result, "Internship report graded successfully."));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private InternshipEvaluationAccessContext AccessContext => new(CurrentUserId, User.IsInRole("Admin") ? "Admin" : User.IsInRole("Lecturer") ? "Lecturer" : "Organization");
}
