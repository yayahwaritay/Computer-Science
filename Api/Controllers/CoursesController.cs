using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    /// <summary>
    /// Create a new course
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Create([FromBody] CourseRequest request)
    {
        var errors = CourseValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<CourseResponse>.FailResponse("Validation failed.", errors));

        var result = await _courseService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CourseResponse>.SuccessResponse(result, "Course created successfully."));
    }

    /// <summary>
    /// Get all courses
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _courseService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CourseResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get courses with pagination
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _courseService.GetPagedAsync(pageNumber, pageSize);
        return Ok(ApiResponse<PagedResponse<CourseResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get course by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _courseService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<CourseResponse>.FailResponse($"Course with ID {id} not found."));

        return Ok(ApiResponse<CourseResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Update a course
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CourseRequest request)
    {
        var errors = CourseValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<CourseResponse>.FailResponse("Validation failed.", errors));

        var result = await _courseService.UpdateAsync(id, request);
        return Ok(ApiResponse<CourseResponse>.SuccessResponse(result, "Course updated successfully."));
    }

    /// <summary>
    /// Delete a course
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _courseService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Course deleted successfully."));
    }
}
