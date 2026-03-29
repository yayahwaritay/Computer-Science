using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface ICourseService
{
    Task<CourseResponse> CreateAsync(CourseRequest request);
    Task<CourseResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<CourseResponse>> GetAllAsync();
    Task<PagedResponse<CourseResponse>> GetPagedAsync(int pageNumber, int pageSize);
    Task<CourseResponse> UpdateAsync(Guid id, CourseRequest request);
    Task DeleteAsync(Guid id);
}
