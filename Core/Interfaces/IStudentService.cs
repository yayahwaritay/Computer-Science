using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface IStudentService
{
    Task<StudentResponse> CreateAsync(StudentRequest request);
    Task<StudentResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<StudentResponse>> GetAllAsync();
    Task<PagedResponse<StudentResponse>> GetPagedAsync(int pageNumber, int pageSize);
    Task<StudentResponse> UpdateAsync(Guid id, StudentRequest request);
    Task DeleteAsync(Guid id);
}
