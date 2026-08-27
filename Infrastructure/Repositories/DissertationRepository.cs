using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class DissertationRepository : GenericRepository<Dissertation>, IDissertationRepository
{
    public DissertationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Dissertation>> GetByStudentIdAsync(string studentId)
    {
        return await _dbSet
            .Where(d => d.StudentId == studentId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Dissertation>> GetByCreatorAsync(Guid createdByUserId)
    {
        return await _dbSet
            .Where(d => d.CreatedByUserId == createdByUserId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(IEnumerable<Dissertation> Data, int TotalCount)> GetPagedByCreatorAsync(Guid createdByUserId, int pageNumber, int pageSize)
    {
        var query = _dbSet.Where(d => d.CreatedByUserId == createdByUserId);

        var totalCount = await query.CountAsync();
        var data = await query
            .AsNoTracking()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
