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
/// Course allocation — which staff member teaches which course, for a given program/year of study,
/// academic year and semester. Managed exclusively by Admin; Admin/Lecturer/Student can read/
/// download. Organization accounts have no access - see InternshipEvaluationsController for what
/// they can reach. A "document" is every row sharing one AcademicYear + Semester (e.g. "2021/2022"
/// + Second = the "2021_2022 Second Semester Allocation").
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lecturer,Student")]
public class CourseAllocationsController : ControllerBase
{
    private readonly ICourseAllocationService _courseAllocationService;

    public CourseAllocationsController(ICourseAllocationService courseAllocationService)
    {
        _courseAllocationService = courseAllocationService;
    }

    /// <summary>
    /// Allocate a single course (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [LogActivity("CourseAllocation", "Create")]
    public async Task<IActionResult> Create([FromBody] CourseAllocationRequest request)
    {
        var errors = CourseAllocationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<CourseAllocationResponse>.FailResponse("Validation failed.", errors));

        var result = await _courseAllocationService.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CourseAllocationResponse>.SuccessResponse(result, "Course allocated successfully."));
    }

    /// <summary>
    /// Allocate a whole table of courses in one call (Admin only) — e.g. one program's full
    /// year-by-year listing, or an entire semester across every program. Keeps allocation simple:
    /// one request instead of one per course row.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBulk([FromBody] CourseAllocationBulkRequest request)
    {
        var result = await _courseAllocationService.CreateBulkAsync(request, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<CourseAllocationResponse>>.SuccessResponse(result, "Course allocations created successfully."));
    }

    /// <summary>
    /// Get all allocations, optionally filtered by academicYear/semester/programName
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? academicYear, [FromQuery] Semester? semester, [FromQuery] string? programName)
    {
        var result = await _courseAllocationService.GetAllAsync(new CourseAllocationFilter
        {
            AcademicYear = academicYear,
            Semester = semester,
            ProgramName = programName
        });

        return Ok(ApiResponse<IEnumerable<CourseAllocationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get allocations with pagination, optionally filtered by academicYear/semester/programName
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? academicYear = null,
        [FromQuery] Semester? semester = null,
        [FromQuery] string? programName = null)
    {
        var result = await _courseAllocationService.GetPagedAsync(pageNumber, pageSize, new CourseAllocationFilter
        {
            AcademicYear = academicYear,
            Semester = semester,
            ProgramName = programName
        });

        return Ok(ApiResponse<PagedResponse<CourseAllocationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get the allocations for the signed-in Lecturer (Lecturer only), optionally narrowed by
    /// academicYear/semester. This is what a Lecturer's "my courses" screen should call.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetMine([FromQuery] string? academicYear, [FromQuery] Semester? semester)
    {
        var result = await _courseAllocationService.GetAllAsync(new CourseAllocationFilter
        {
            AcademicYear = academicYear,
            Semester = semester,
            LecturerUserId = CurrentUserId
        });

        return Ok(ApiResponse<IEnumerable<CourseAllocationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Download the compiled allocation document as a PDF, in the standard multi-program layout
    /// (e.g. academicYear=2021/2022&amp;semester=Second downloads as
    /// "2021_2022 Second Semester Allocation.pdf"). Optionally narrow to one program.
    /// </summary>
    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] string academicYear, [FromQuery] Semester semester, [FromQuery] string? programName)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return BadRequest(ApiResponse<object>.FailResponse("academicYear is required, e.g. \"2021/2022\"."));

        var bytes = await _courseAllocationService.ExportPdfAsync(new CourseAllocationFilter
        {
            AcademicYear = academicYear,
            Semester = semester,
            ProgramName = programName
        });

        return File(bytes, "application/pdf", BuildFileName(academicYear, semester));
    }

    /// <summary>
    /// Download the signed-in Lecturer's own slice of the compiled allocation document as a PDF
    /// (Lecturer only) — same layout and file naming as /export/pdf, containing only their rows.
    /// </summary>
    [HttpGet("mine/export/pdf")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> ExportMyPdf([FromQuery] string academicYear, [FromQuery] Semester semester)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return BadRequest(ApiResponse<object>.FailResponse("academicYear is required, e.g. \"2021/2022\"."));

        var bytes = await _courseAllocationService.ExportPdfAsync(new CourseAllocationFilter
        {
            AcademicYear = academicYear,
            Semester = semester,
            LecturerUserId = CurrentUserId
        });

        return File(bytes, "application/pdf", BuildFileName(academicYear, semester));
    }

    /// <summary>
    /// Get an allocation by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _courseAllocationService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<CourseAllocationResponse>.FailResponse($"Course allocation with ID {id} not found."));

        return Ok(ApiResponse<CourseAllocationResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Update an allocation (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [LogActivity("CourseAllocation", "Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CourseAllocationRequest request)
    {
        var errors = CourseAllocationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<CourseAllocationResponse>.FailResponse("Validation failed.", errors));

        var result = await _courseAllocationService.UpdateAsync(id, request);
        return Ok(ApiResponse<CourseAllocationResponse>.SuccessResponse(result, "Course allocation updated successfully."));
    }

    /// <summary>
    /// Delete an allocation (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [LogActivity("CourseAllocation", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _courseAllocationService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Course allocation deleted successfully."));
    }

    private static string BuildFileName(string academicYear, Semester semester)
    {
        return $"{academicYear.Replace('/', '_')} {semester} Semester Allocation.pdf";
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
