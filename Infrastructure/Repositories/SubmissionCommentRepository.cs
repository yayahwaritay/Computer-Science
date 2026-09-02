using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class SubmissionCommentRepository : GenericRepository<SubmissionComment>, ISubmissionCommentRepository
{
    public SubmissionCommentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<SubmissionComment>> GetBySubmissionIdAsync(Guid studentSubmissionId)
    {
        return await _dbSet.AsNoTracking()
            .Where(c => c.StudentSubmissionId == studentSubmissionId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }
}
