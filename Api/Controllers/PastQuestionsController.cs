using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PastQuestionsController : ControllerBase
{
    private readonly IPastQuestionService _pastQuestionService;

    public PastQuestionsController(IPastQuestionService pastQuestionService)
    {
        _pastQuestionService = pastQuestionService;
    }

    /// <summary>
    /// Upload a past question (PDF only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Lecturer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] string courseName, [FromForm] string courseCode, IFormFile file)
    {
        var request = new PastQuestionRequest { CourseName = courseName, CourseCode = courseCode };

        var errors = PastQuestionValidator.Validate(request);
        if (file == null || file.Length == 0)
            errors.Add("File is required.");

        if (errors.Any())
            return BadRequest(ApiResponse<PastQuestionResponse>.FailResponse("Validation failed.", errors));

        var result = await _pastQuestionService.CreateAsync(request, file!);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<PastQuestionResponse>.SuccessResponse(result, "Past question uploaded successfully."));
    }

    /// <summary>
    /// Get all past questions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _pastQuestionService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PastQuestionResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get past questions with pagination
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _pastQuestionService.GetPagedAsync(pageNumber, pageSize);
        return Ok(ApiResponse<PagedResponse<PastQuestionResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get past question by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _pastQuestionService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<PastQuestionResponse>.FailResponse($"Past question with ID {id} not found."));

        return Ok(ApiResponse<PastQuestionResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Download past question file
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var (fileBytes, contentType, fileName) = await _pastQuestionService.DownloadAsync(id);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// Update a past question
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid id, [FromForm] string courseName, [FromForm] string courseCode, IFormFile? file)
    {
        var request = new PastQuestionRequest { CourseName = courseName, CourseCode = courseCode };

        var errors = PastQuestionValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<PastQuestionResponse>.FailResponse("Validation failed.", errors));

        var result = await _pastQuestionService.UpdateAsync(id, request, file);
        return Ok(ApiResponse<PastQuestionResponse>.SuccessResponse(result, "Past question updated successfully."));
    }

    /// <summary>
    /// Delete a past question
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _pastQuestionService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Past question deleted successfully."));
    }
}
