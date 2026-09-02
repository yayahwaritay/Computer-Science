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
/// The internship placement record for one student per academic year + semester: which host
/// Organization they're placed with, and which Lecturer grades their report. Managed by Admin.
/// This is what scopes both sides' visibility - Organization only ever sees the students placed
/// with it (via GET /mine), Lecturer only the students allocated to it (also via GET /mine); the
/// unfiltered list/lookup endpoints are Admin/Lecturer only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lecturer,Organization")]
public class InternshipAllocationsController : ControllerBase
{
    private readonly IInternshipAllocationService _internshipAllocationService;

    public InternshipAllocationsController(IInternshipAllocationService internshipAllocationService)
    {
        _internshipAllocationService = internshipAllocationService;
    }

    /// <summary>
    /// Allocate the internship-supervising lecturer for one student (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [LogActivity("InternshipAllocation", "Create")]
    public async Task<IActionResult> Create([FromBody] InternshipAllocationRequest request)
    {
        var errors = InternshipAllocationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<InternshipAllocationResponse>.FailResponse("Validation failed.", errors));

        var result = await _internshipAllocationService.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<InternshipAllocationResponse>.SuccessResponse(result, "Internship allocation created successfully."));
    }

    /// <summary>
    /// Get all allocations, optionally filtered by academicYear/semester/lecturerUserId/studentId (Admin/Lecturer only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? academicYear,
        [FromQuery] Semester? semester,
        [FromQuery] Guid? lecturerUserId,
        [FromQuery] string? studentId)
    {
        var result = await _internshipAllocationService.GetAllAsync(new InternshipAllocationFilter
        {
            AcademicYear = academicYear,
            Semester = semester,
            LecturerUserId = lecturerUserId,
            StudentId = studentId
        });

        return Ok(ApiResponse<IEnumerable<InternshipAllocationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get the signed-in caller's own side of the placement, optionally narrowed by
    /// academicYear/semester: a Lecturer gets the students allocated to it for report grading;
    /// an Organization gets the students placed with it for evaluation - this is what an
    /// Organization's "students I can evaluate" screen should call, since Organization has no
    /// access to the general student list or the unfiltered GET above.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Lecturer,Organization")]
    public async Task<IActionResult> GetMine([FromQuery] string? academicYear, [FromQuery] Semester? semester)
    {
        var filter = new InternshipAllocationFilter { AcademicYear = academicYear, Semester = semester };
        if (User.IsInRole("Lecturer"))
            filter.LecturerUserId = CurrentUserId;
        else
            filter.OrganizationUserId = CurrentUserId;

        var result = await _internshipAllocationService.GetAllAsync(filter);
        return Ok(ApiResponse<IEnumerable<InternshipAllocationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get an allocation by ID (Admin/Lecturer only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _internshipAllocationService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<InternshipAllocationResponse>.FailResponse($"Internship allocation with ID {id} not found."));

        return Ok(ApiResponse<InternshipAllocationResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Reassign an allocation (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [LogActivity("InternshipAllocation", "Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] InternshipAllocationRequest request)
    {
        var errors = InternshipAllocationValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<InternshipAllocationResponse>.FailResponse("Validation failed.", errors));

        var result = await _internshipAllocationService.UpdateAsync(id, request);
        return Ok(ApiResponse<InternshipAllocationResponse>.SuccessResponse(result, "Internship allocation updated successfully."));
    }

    /// <summary>
    /// Delete an allocation (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [LogActivity("InternshipAllocation", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _internshipAllocationService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Internship allocation deleted successfully."));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
