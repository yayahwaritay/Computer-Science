using CompSci.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace CompSci.Core.Interfaces;

public interface IDissertationService
{
    Task<DissertationResponse> CreateAsync(DissertationRequest request, IFormFile file, Guid createdByUserId);
    Task<DissertationResponse?> GetByIdAsync(Guid id, DissertationAccessContext access);
    Task<IEnumerable<DissertationResponse>> GetAllAsync(DissertationAccessContext access);
    Task<PagedResponse<DissertationResponse>> GetPagedAsync(int pageNumber, int pageSize, DissertationAccessContext access);
    Task<IEnumerable<DissertationResponse>> GetByStudentIdAsync(string studentId, DissertationAccessContext access);
    Task<DissertationResponse> UpdateAsync(Guid id, DissertationRequest request, IFormFile? file, DissertationAccessContext access);
    Task DeleteAsync(Guid id, DissertationAccessContext access);
    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAsync(Guid id, DissertationAccessContext access);

    /// <summary>
    /// Admin-only cross-cutting search across every lecturer's dissertation records, filtered by
    /// academic year range / program / department / school.
    /// </summary>
    Task<IEnumerable<DissertationResponse>> SearchAsync(DissertationFilter filter);
    Task<byte[]> ExportCsvAsync(DissertationFilter filter);
    Task<byte[]> ExportPdfAsync(DissertationFilter filter);
}
