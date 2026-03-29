using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface INoteRepository : IGenericRepository<Note>
{
    Task<IEnumerable<Note>> GetByCourseCodeAsync(string courseCode);
}
