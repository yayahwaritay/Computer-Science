using System.Security.Claims;
using CompSci.Api.Filters;
using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Repository of final year project / dissertation submissions. Managed exclusively by
/// Admin/Lecturer as the official academic record kept before a student graduates.
/// Admins can see/manage every record; Lecturers are scoped to only the records they created.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lecturer")]
public class DissertationsController : ControllerBase
{
    private readonly IDissertationService _dissertationService;

    public DissertationsController(IDissertationService dissertationService)
    {
        _dissertationService = dissertationService;
    }

    /// <summary>
    /// Record a student's final year project/dissertation, with its full documentation
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [LogActivity("Dissertation", "Create")]
    public async Task<IActionResult> Create([FromForm] DissertationRequest request, IFormFile file)
    {
        var errors = DissertationValidator.Validate(request);
        if (file == null || file.Length == 0)
            errors.Add("File is required.");

        if (errors.Any())
            return BadRequest(ApiResponse<DissertationResponse>.FailResponse("Validation failed.", errors));

        var result = await _dissertationService.CreateAsync(request, file!, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DissertationResponse>.SuccessResponse(result, "Dissertation record created successfully."));
    }

    /// <summary>
    /// Get all dissertation records visible to the caller (Admin: all, Lecturer: only their own)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _dissertationService.GetAllAsync(AccessContext);
        return Ok(ApiResponse<IEnumerable<DissertationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get dissertation records with pagination (Admin: all, Lecturer: only their own)
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _dissertationService.GetPagedAsync(pageNumber, pageSize, AccessContext);
        return Ok(ApiResponse<PagedResponse<DissertationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Cross-cutting search across every lecturer's dissertation records, filtered by academic
    /// year range / program / department / school (Admin only). All filters are optional and combine.
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Search(
        [FromQuery] int? fromYear,
        [FromQuery] int? toYear,
        [FromQuery] string? program,
        [FromQuery] string? department,
        [FromQuery] string? school)
    {
        var result = await _dissertationService.SearchAsync(new DissertationFilter
        {
            FromYear = fromYear,
            ToYear = toYear,
            Program = program,
            Department = department,
            School = school
        });

        return Ok(ApiResponse<IEnumerable<DissertationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Download a compiled CSV of matching dissertation records (Admin only). Same filters as
    /// /search. Contains only: Student Name, Student ID, Program, Topic, Academic Year.
    /// </summary>
    [HttpGet("export/csv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] int? fromYear,
        [FromQuery] int? toYear,
        [FromQuery] string? program,
        [FromQuery] string? department,
        [FromQuery] string? school)
    {
        var bytes = await _dissertationService.ExportCsvAsync(new DissertationFilter
        {
            FromYear = fromYear,
            ToYear = toYear,
            Program = program,
            Department = department,
            School = school
        });

        return File(bytes, "text/csv", $"dissertations_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    /// <summary>
    /// Download a compiled PDF of matching dissertation records (Admin only). Same filters as
    /// /search. Contains only: Student Name, Student ID, Program, Topic, Academic Year.
    /// </summary>
    [HttpGet("export/pdf")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] int? fromYear,
        [FromQuery] int? toYear,
        [FromQuery] string? program,
        [FromQuery] string? department,
        [FromQuery] string? school)
    {
        var bytes = await _dissertationService.ExportPdfAsync(new DissertationFilter
        {
            FromYear = fromYear,
            ToYear = toYear,
            Program = program,
            Department = department,
            School = school
        });

        return File(bytes, "application/pdf", $"dissertations_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    /// <summary>
    /// Get a dissertation record by ID (Lecturer: only if they created it)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _dissertationService.GetByIdAsync(id, AccessContext);
        if (result == null)
            return NotFound(ApiResponse<DissertationResponse>.FailResponse($"Dissertation with ID {id} not found."));

        return Ok(ApiResponse<DissertationResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Get dissertation records for a given Student ID (Lecturer: only their own). Student IDs may
    /// contain '/', so this is a query parameter rather than a route segment
    /// (e.g. /api/dissertations/by-student?studentId=CS%2F2026%2F998).
    /// </summary>
    [HttpGet("by-student")]
    public async Task<IActionResult> GetByStudentId([FromQuery] string studentId)
    {
        var result = await _dissertationService.GetByStudentIdAsync(studentId, AccessContext);
        return Ok(ApiResponse<IEnumerable<DissertationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Download the dissertation/project documentation file (Lecturer: only if they created it)
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var (fileBytes, contentType, fileName) = await _dissertationService.DownloadAsync(id, AccessContext);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// Update a dissertation record (Lecturer: only if they created it)
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    [LogActivity("Dissertation", "Update")]
    public async Task<IActionResult> Update(Guid id, [FromForm] DissertationRequest request, IFormFile? file)
    {
        var errors = DissertationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<DissertationResponse>.FailResponse("Validation failed.", errors));

        var result = await _dissertationService.UpdateAsync(id, request, file, AccessContext);
        return Ok(ApiResponse<DissertationResponse>.SuccessResponse(result, "Dissertation record updated successfully."));
    }

    /// <summary>
    /// Delete a dissertation record (Lecturer: only if they created it)
    /// </summary>
    [HttpDelete("{id}")]
    [LogActivity("Dissertation", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _dissertationService.DeleteAsync(id, AccessContext);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Dissertation record deleted successfully."));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private DissertationAccessContext AccessContext => new(CurrentUserId, User.IsInRole("Admin"));
}
