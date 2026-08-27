using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class ActivityLogRepository : GenericRepository<ActivityLog>, IActivityLogRepository
{
    public ActivityLogRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<ActivityLog> Data, int TotalCount)> GetPagedFilteredAsync(
        int pageNumber, int pageSize, Guid? userId, string? entityType)
    {
        var query = _dbSet.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        query = query.OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync();
        var data = await query
            .AsNoTracking()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
