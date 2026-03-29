using CompSci.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Interfaces;

public interface IPastQuestionService
{
    Task<PastQuestionResponse> CreateAsync(PastQuestionRequest request, IFormFile file);
    Task<PastQuestionResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<PastQuestionResponse>> GetAllAsync();
    Task<PagedResponse<PastQuestionResponse>> GetPagedAsync(int pageNumber, int pageSize);
    Task<PastQuestionResponse> UpdateAsync(Guid id, PastQuestionRequest request, IFormFile? file);
    Task DeleteAsync(Guid id);
    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id);
}
