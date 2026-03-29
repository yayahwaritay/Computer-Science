using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface ICourseRepository : IGenericRepository<Course>
{
    Task<Course?> GetByCourseCodeAsync(string courseCode);
    Task<bool> CourseCodeExistsAsync(string courseCode);
}
