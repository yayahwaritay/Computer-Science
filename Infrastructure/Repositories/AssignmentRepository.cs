using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class AssignmentRepository : GenericRepository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Assignment>> GetByCourseCodeAsync(string courseCode)
    {
        return await _dbSet
            .Where(a => a.CourseCode == courseCode)
            .AsNoTracking()
            .ToListAsync();
    }
}
