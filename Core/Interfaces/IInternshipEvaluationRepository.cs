using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IInternshipEvaluationRepository : IGenericRepository<InternshipEvaluation>
{
    Task<IEnumerable<InternshipEvaluation>> GetByStudentIdAsync(Guid studentId);
    Task<IEnumerable<InternshipEvaluation>> GetByOrganizationAsync(Guid organizationUserId);
    Task<IEnumerable<InternshipEvaluation>> GetByAllocatedLecturerAsync(Guid lecturerUserId);
}
