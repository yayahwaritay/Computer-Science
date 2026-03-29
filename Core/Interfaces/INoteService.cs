using CompSci.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Interfaces;

public interface INoteService
{
    Task<NoteResponse> CreateAsync(NoteRequest request, IFormFile file);
    Task<NoteResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<NoteResponse>> GetAllAsync();
    Task<PagedResponse<NoteResponse>> GetPagedAsync(int pageNumber, int pageSize);
    Task<NoteResponse> UpdateAsync(Guid id, NoteRequest request, IFormFile? file);
    Task DeleteAsync(Guid id);
    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id);
}
