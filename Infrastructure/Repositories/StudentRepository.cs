using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class StudentRepository : GenericRepository<Student>, IStudentRepository
{
    public StudentRepository(AppDbContext context) : base(context) { }

    public async Task<Student?> GetByStudentIdAsync(string studentId)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.StudentId == studentId);
    }

    public async Task<bool> StudentIdExistsAsync(string studentId)
    {
        return await _dbSet.AnyAsync(s => s.StudentId == studentId);
    }
}
