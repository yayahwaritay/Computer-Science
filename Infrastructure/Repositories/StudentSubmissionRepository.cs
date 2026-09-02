using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class StudentSubmissionRepository : GenericRepository<StudentSubmission>, IStudentSubmissionRepository
{
    public StudentSubmissionRepository(AppDbContext context) : base(context) { }

    public async Task<StudentSubmission?> GetForStudentAndTypeAsync(Guid studentId, SubmissionType type)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.StudentId == studentId && s.Type == type);
    }

    public async Task<IEnumerable<StudentSubmission>> GetAllByTypeAsync(SubmissionType type)
    {
        return await _dbSet.AsNoTracking().Where(s => s.Type == type).ToListAsync();
    }
}
