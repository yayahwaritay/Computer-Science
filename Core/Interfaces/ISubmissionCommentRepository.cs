using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface ISubmissionCommentRepository : IGenericRepository<SubmissionComment>
{
    Task<IEnumerable<SubmissionComment>> GetBySubmissionIdAsync(Guid studentSubmissionId);
}
