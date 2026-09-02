using CompSci.Core.Entities;
using CompSci.Core.Enums;

namespace CompSci.Core.Interfaces;

public interface IStudentSubmissionRepository : IGenericRepository<StudentSubmission>
{
    Task<StudentSubmission?> GetForStudentAndTypeAsync(Guid studentId, SubmissionType type);
    Task<IEnumerable<StudentSubmission>> GetAllByTypeAsync(SubmissionType type);
}
