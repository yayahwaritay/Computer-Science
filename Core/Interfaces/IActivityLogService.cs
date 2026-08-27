using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(Guid userId, string entityType, string action, Guid entityId);
    Task<PagedResponse<ActivityLogResponse>> GetPagedAsync(int pageNumber, int pageSize, Guid? userId, string? entityType);
}
