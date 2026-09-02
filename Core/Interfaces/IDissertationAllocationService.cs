using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface IDissertationAllocationService
{
    Task<DissertationAllocationResponse> CreateAsync(DissertationAllocationRequest request, Guid createdByUserId);
    Task<DissertationAllocationResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<DissertationAllocationResponse>> GetAllAsync(DissertationAllocationFilter filter);
    Task<DissertationAllocationResponse> UpdateAsync(Guid id, DissertationAllocationRequest request);
    Task DeleteAsync(Guid id);
}
