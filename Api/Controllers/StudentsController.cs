using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>
    /// Create a new student
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Create([FromBody] StudentRequest request)
    {
        var errors = StudentValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<StudentResponse>.FailResponse("Validation failed.", errors));

        var result = await _studentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<StudentResponse>.SuccessResponse(result, "Student created successfully."));
    }

    /// <summary>
    /// Get all students
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _studentService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<StudentResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get students with pagination
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _studentService.GetPagedAsync(pageNumber, pageSize);
        return Ok(ApiResponse<PagedResponse<StudentResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _studentService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<StudentResponse>.FailResponse($"Student with ID {id} not found."));

        return Ok(ApiResponse<StudentResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Update a student
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Update(Guid id, [FromBody] StudentRequest request)
    {
        var errors = StudentValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<StudentResponse>.FailResponse("Validation failed.", errors));

        var result = await _studentService.UpdateAsync(id, request);
        return Ok(ApiResponse<StudentResponse>.SuccessResponse(result, "Student updated successfully."));
    }

    /// <summary>
    /// Delete a student
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _studentService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Student deleted successfully."));
    }
}
