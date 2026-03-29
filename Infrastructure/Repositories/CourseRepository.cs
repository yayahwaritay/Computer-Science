using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class CourseRepository : GenericRepository<Course>, ICourseRepository
{
    public CourseRepository(AppDbContext context) : base(context) { }

    public async Task<Course?> GetByCourseCodeAsync(string courseCode)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.CourseCode == courseCode);
    }

    public async Task<bool> CourseCodeExistsAsync(string courseCode)
    {
        return await _dbSet.AnyAsync(c => c.CourseCode == courseCode);
    }
}
