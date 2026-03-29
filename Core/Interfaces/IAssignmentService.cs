using CompSci.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Interfaces;

public interface IAssignmentService
{
    Task<AssignmentResponse> CreateAsync(AssignmentRequest request, IFormFile? file);
    Task<AssignmentResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<AssignmentResponse>> GetAllAsync();
    Task<PagedResponse<AssignmentResponse>> GetPagedAsync(int pageNumber, int pageSize);
    Task<AssignmentResponse> UpdateAsync(Guid id, AssignmentRequest request, IFormFile? file);
    Task DeleteAsync(Guid id);
    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id);
}
