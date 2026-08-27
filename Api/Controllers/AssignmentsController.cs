using CompSci.Api.Filters;
using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public AssignmentsController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Create a new assignment with optional file upload
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Lecturer")]
    [Consumes("multipart/form-data")]
    [LogActivity("Assignment", "Create")]
    public async Task<IActionResult> Create([FromForm] string courseName, [FromForm] string courseCode, [FromForm] string assignmentTitle, [FromForm] int importance, [FromForm] DateTime dueDate, IFormFile? file)
    {
        var request = new AssignmentRequest
        {
            CourseName = courseName,
            CourseCode = courseCode,
            AssignmentTitle = assignmentTitle,
            Importance = (CompSci.Core.Enums.Importance)importance,
            DueDate = dueDate
        };

        var errors = AssignmentValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<AssignmentResponse>.FailResponse("Validation failed.", errors));

        var result = await _assignmentService.CreateAsync(request, file);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<AssignmentResponse>.SuccessResponse(result, "Assignment created successfully."));
    }

    /// <summary>
    /// Get all assignments
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _assignmentService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<AssignmentResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get assignments with pagination
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _assignmentService.GetPagedAsync(pageNumber, pageSize);
        return Ok(ApiResponse<PagedResponse<AssignmentResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get assignment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _assignmentService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<AssignmentResponse>.FailResponse($"Assignment with ID {id} not found."));

        return Ok(ApiResponse<AssignmentResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Download assignment file
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var (fileBytes, contentType, fileName) = await _assignmentService.DownloadAsync(id);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// Update an assignment with optional file upload
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    [Consumes("multipart/form-data")]
    [LogActivity("Assignment", "Update")]
    public async Task<IActionResult> Update(Guid id, [FromForm] string courseName, [FromForm] string courseCode, [FromForm] string assignmentTitle, [FromForm] int importance, [FromForm] DateTime dueDate, IFormFile? file)
    {
        var request = new AssignmentRequest
        {
            CourseName = courseName,
            CourseCode = courseCode,
            AssignmentTitle = assignmentTitle,
            Importance = (CompSci.Core.Enums.Importance)importance,
            DueDate = dueDate
        };

        var errors = AssignmentValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<AssignmentResponse>.FailResponse("Validation failed.", errors));

        var result = await _assignmentService.UpdateAsync(id, request, file);
        return Ok(ApiResponse<AssignmentResponse>.SuccessResponse(result, "Assignment updated successfully."));
    }

    /// <summary>
    /// Delete an assignment
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    [LogActivity("Assignment", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _assignmentService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Assignment deleted successfully."));
    }
}
