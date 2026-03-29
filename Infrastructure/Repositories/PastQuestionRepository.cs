using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class PastQuestionRepository : GenericRepository<PastQuestion>, IPastQuestionRepository
{
    public PastQuestionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<PastQuestion>> GetByCourseCodeAsync(string courseCode)
    {
        return await _dbSet
            .Where(p => p.CourseCode == courseCode)
            .AsNoTracking()
            .ToListAsync();
    }
}
