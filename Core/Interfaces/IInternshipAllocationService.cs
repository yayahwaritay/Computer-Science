using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface IInternshipAllocationService
{
    Task<InternshipAllocationResponse> CreateAsync(InternshipAllocationRequest request, Guid createdByUserId);
    Task<InternshipAllocationResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<InternshipAllocationResponse>> GetAllAsync(InternshipAllocationFilter filter);
    Task<InternshipAllocationResponse> UpdateAsync(Guid id, InternshipAllocationRequest request);
    Task DeleteAsync(Guid id);
}
