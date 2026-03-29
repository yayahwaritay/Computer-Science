using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IStudentRepository : IGenericRepository<Student>
{
    Task<Student?> GetByStudentIdAsync(string studentId);
    Task<bool> StudentIdExistsAsync(string studentId);
}
