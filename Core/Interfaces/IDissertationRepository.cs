using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IDissertationRepository : IGenericRepository<Dissertation>
{
    Task<IEnumerable<Dissertation>> GetByStudentIdAsync(string studentId);
    Task<IEnumerable<Dissertation>> GetByCreatorAsync(Guid createdByUserId);
    Task<(IEnumerable<Dissertation> Data, int TotalCount)> GetPagedByCreatorAsync(Guid createdByUserId, int pageNumber, int pageSize);
}
