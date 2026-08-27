using System.Security.Claims;
using CompSci.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompSci.Api.Filters;

/// <summary>
/// Records a site-wide activity log entry after a Create/Update/Delete action succeeds, attributing
/// it to the authenticated caller (Admin or Lecturer). Apply to write endpoints, e.g.
/// [LogActivity("Dissertation", "Create")]. Logging never blocks or fails the request itself.
/// </summary>
public class LogActivityAttribute : TypeFilterAttribute
{
    public LogActivityAttribute(string entityType, string action) : base(typeof(LogActivityFilter))
    {
        Arguments = new object[] { entityType, action };
    }
}

public class LogActivityFilter : IAsyncActionFilter
{
    private readonly string _entityType;
    private readonly string _action;
    private readonly IActivityLogService _activityLogService;

    public LogActivityFilter(string entityType, string action, IActivityLogService activityLogService)
    {
        _entityType = entityType;
        _action = action;
        _activityLogService = activityLogService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();

        if (executedContext.Exception != null && !executedContext.ExceptionHandled)
            return;

        var entityId = ExtractEntityId(context, executedContext);
        if (entityId == null)
            return;

        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return;

        await _activityLogService.LogAsync(userId, _entityType, _action, entityId.Value);
    }

    private static Guid? ExtractEntityId(ActionExecutingContext context, ActionExecutedContext executedContext)
    {
        // Update/Delete actions take the record's ID as an "id" route/action parameter.
        if (context.ActionArguments.TryGetValue("id", out var idArg) && idArg is Guid idFromArgs)
            return idFromArgs;

        // Create actions return CreatedAtAction(..., new { id = result.Id }, ...).
        if (executedContext.Result is CreatedAtActionResult { RouteValues: not null } created &&
            created.RouteValues.TryGetValue("id", out var idFromRoute) &&
            idFromRoute is Guid createdId)
            return createdId;

        return null;
    }
}
