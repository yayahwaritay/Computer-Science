using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class InternshipEvaluationRepository : GenericRepository<InternshipEvaluation>, IInternshipEvaluationRepository
{
    public InternshipEvaluationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<InternshipEvaluation>> GetByStudentIdAsync(Guid studentId)
    {
        return await _dbSet.AsNoTracking().Where(e => e.StudentId == studentId).ToListAsync();
    }

    public async Task<IEnumerable<InternshipEvaluation>> GetByOrganizationAsync(Guid organizationUserId)
    {
        return await _dbSet.AsNoTracking().Where(e => e.OrganizationUserId == organizationUserId).ToListAsync();
    }

    public async Task<IEnumerable<InternshipEvaluation>> GetByAllocatedLecturerAsync(Guid lecturerUserId)
    {
        return await _dbSet.AsNoTracking().Where(e => e.AllocatedLecturerUserId == lecturerUserId).ToListAsync();
    }
}
