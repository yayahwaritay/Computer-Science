using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Interfaces;

namespace CompSci.Core.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivityLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(Guid userId, string entityType, string action, Guid entityId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return;

        var log = new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Username = user.Username,
            UserRole = user.Role.ToString(),
            LecturerId = user.LecturerId,
            EntityType = entityType,
            Action = action,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow
        };

        await _unitOfWork.ActivityLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PagedResponse<ActivityLogResponse>> GetPagedAsync(int pageNumber, int pageSize, Guid? userId, string? entityType)
    {
        var (data, totalCount) = await _unitOfWork.ActivityLogs.GetPagedFilteredAsync(pageNumber, pageSize, userId, entityType);

        return new PagedResponse<ActivityLogResponse>
        {
            Data = data.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static ActivityLogResponse MapToResponse(ActivityLog log)
    {
        return new ActivityLogResponse
        {
            Id = log.Id,
            UserId = log.UserId,
            Username = log.Username,
            UserRole = log.UserRole,
            LecturerId = log.LecturerId,
            EntityType = log.EntityType,
            Action = log.Action,
            EntityId = log.EntityId,
            Timestamp = log.Timestamp
        };
    }
}
