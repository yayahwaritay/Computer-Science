using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IActivityLogRepository : IGenericRepository<ActivityLog>
{
    Task<(IEnumerable<ActivityLog> Data, int TotalCount)> GetPagedFilteredAsync(
        int pageNumber, int pageSize, Guid? userId, string? entityType);
}
