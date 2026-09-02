using System.Security.Claims;
using CompSci.Api.Filters;
using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Assigns the supervising Lecturer for one student's dissertation/final-year-project write-up, per
/// academic year. This is what scopes access to that student's uploaded write-up (see
/// DissertationSubmissionsController) - only Admin and the Lecturer assigned here can see/comment on
/// it. Managed by Admin, mirroring InternshipAllocationsController's role for internship reports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lecturer")]
public class DissertationAllocationsController : ControllerBase
{
    private readonly IDissertationAllocationService _dissertationAllocationService;

    public DissertationAllocationsController(IDissertationAllocationService dissertationAllocationService)
    {
        _dissertationAllocationService = dissertationAllocationService;
    }

    /// <summary>
    /// Assign the dissertation-supervising lecturer for one student (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [LogActivity("DissertationAllocation", "Create")]
    public async Task<IActionResult> Create([FromBody] DissertationAllocationRequest request)
    {
        var errors = DissertationAllocationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<DissertationAllocationResponse>.FailResponse("Validation failed.", errors));

        var result = await _dissertationAllocationService.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DissertationAllocationResponse>.SuccessResponse(result, "Dissertation supervisor assigned successfully."));
    }

    /// <summary>
    /// Get all allocations, optionally filtered by academicYear/lecturerUserId/studentId (Admin/Lecturer only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? academicYear,
        [FromQuery] Guid? lecturerUserId,
        [FromQuery] string? studentId)
    {
        var result = await _dissertationAllocationService.GetAllAsync(new DissertationAllocationFilter
        {
            AcademicYear = academicYear,
            LecturerUserId = lecturerUserId,
            StudentId = studentId
        });

        return Ok(ApiResponse<IEnumerable<DissertationAllocationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get the signed-in Lecturer's own assigned students, optionally narrowed by academicYear
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetMine([FromQuery] string? academicYear)
    {
        var result = await _dissertationAllocationService.GetAllAsync(new DissertationAllocationFilter
        {
            AcademicYear = academicYear,
            LecturerUserId = CurrentUserId
        });

        return Ok(ApiResponse<IEnumerable<DissertationAllocationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get an allocation by ID (Admin/Lecturer only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _dissertationAllocationService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<DissertationAllocationResponse>.FailResponse($"Dissertation allocation with ID {id} not found."));

        return Ok(ApiResponse<DissertationAllocationResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Reassign an allocation (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [LogActivity("DissertationAllocation", "Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DissertationAllocationRequest request)
    {
        var errors = DissertationAllocationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<DissertationAllocationResponse>.FailResponse("Validation failed.", errors));

        var result = await _dissertationAllocationService.UpdateAsync(id, request);
        return Ok(ApiResponse<DissertationAllocationResponse>.SuccessResponse(result, "Dissertation allocation updated successfully."));
    }

    /// <summary>
    /// Delete an allocation (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [LogActivity("DissertationAllocation", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _dissertationAllocationService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Dissertation allocation deleted successfully."));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
