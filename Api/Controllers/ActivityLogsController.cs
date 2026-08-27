using System.Security.Claims;
using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

/// <summary>
/// Site-wide activity/audit log: who (Admin/Lecturer) performed which create/update/delete
/// action, on what record, and when.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lecturer")]
public class ActivityLogsController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogsController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Get every activity log entry, optionally filtered by user and/or entity type (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? entityType = null)
    {
        var result = await _activityLogService.GetPagedAsync(pageNumber, pageSize, userId, entityType);
        return Ok(ApiResponse<PagedResponse<ActivityLogResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Get the current user's own activity log entries (Admin or Lecturer)
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _activityLogService.GetPagedAsync(pageNumber, pageSize, userId, entityType);
        return Ok(ApiResponse<PagedResponse<ActivityLogResponse>>.SuccessResponse(result));
    }
}
