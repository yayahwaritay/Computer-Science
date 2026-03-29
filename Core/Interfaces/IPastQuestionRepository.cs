using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IPastQuestionRepository : IGenericRepository<PastQuestion>
{
    Task<IEnumerable<PastQuestion>> GetByCourseCodeAsync(string courseCode);
}
