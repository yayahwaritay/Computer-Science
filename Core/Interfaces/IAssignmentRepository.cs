using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IAssignmentRepository : IGenericRepository<Assignment>
{
    Task<IEnumerable<Assignment>> GetByCourseCodeAsync(string courseCode);
}
