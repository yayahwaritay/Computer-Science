using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    /// <summary>
    /// Upload a note (PDF or DOCX)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Lecturer,Student")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] string courseName, [FromForm] string courseCode, IFormFile file)
    {
        var request = new NoteRequest { CourseName = courseName, CourseCode = courseCode };

        var errors = NoteValidator.Validate(request);
        if (file == null || file.Length == 0)
            errors.Add("File is required.");

        if (errors.Any())
            return BadRequest(ApiResponse<NoteResponse>.FailResponse("Validation failed.", errors));

        var result = await _noteService.CreateAsync(request, file!);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<NoteResponse>.SuccessResponse(result, "Note uploaded successfully."));
    }

    /// <summary>
    /// Get all notes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _noteService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<NoteResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get notes with pagination
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _noteService.GetPagedAsync(pageNumber, pageSize);
        return Ok(ApiResponse<PagedResponse<NoteResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get note by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _noteService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<NoteResponse>.FailResponse($"Note with ID {id} not found."));

        return Ok(ApiResponse<NoteResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Download note file
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var (fileBytes, contentType, fileName) = await _noteService.DownloadAsync(id);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// Update a note
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid id, [FromForm] string courseName, [FromForm] string courseCode, IFormFile? file)
    {
        var request = new NoteRequest { CourseName = courseName, CourseCode = courseCode };

        var errors = NoteValidator.Validate(request);
        if (errors.Any())
            return BadRequest(ApiResponse<NoteResponse>.FailResponse("Validation failed.", errors));

        var result = await _noteService.UpdateAsync(id, request, file);
        return Ok(ApiResponse<NoteResponse>.SuccessResponse(result, "Note updated successfully."));
    }

    /// <summary>
    /// Delete a note
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _noteService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Note deleted successfully."));
    }
}
