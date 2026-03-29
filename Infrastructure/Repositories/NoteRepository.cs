using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class NoteRepository : GenericRepository<Note>, INoteRepository
{
    public NoteRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Note>> GetByCourseCodeAsync(string courseCode)
    {
        return await _dbSet
            .Where(n => n.CourseCode == courseCode)
            .AsNoTracking()
            .ToListAsync();
    }
}
