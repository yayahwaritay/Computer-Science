using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class InternshipAllocationRepository : GenericRepository<InternshipAllocation>, IInternshipAllocationRepository
{
    public InternshipAllocationRepository(AppDbContext context) : base(context) { }

    public async Task<InternshipAllocation?> GetForStudentPeriodAsync(Guid studentId, string academicYear, Semester semester)
    {
        return await _dbSet.FirstOrDefaultAsync(a =>
            a.StudentId == studentId && a.AcademicYear == academicYear && a.Semester == semester);
    }
}
