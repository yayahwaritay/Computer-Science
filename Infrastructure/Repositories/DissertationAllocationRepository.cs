using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class DissertationAllocationRepository : GenericRepository<DissertationAllocation>, IDissertationAllocationRepository
{
    public DissertationAllocationRepository(AppDbContext context) : base(context) { }

    public async Task<DissertationAllocation?> GetForStudentYearAsync(Guid studentId, string academicYear)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.StudentId == studentId && a.AcademicYear == academicYear);
    }
}
