using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface ICourseAllocationService
{
    Task<CourseAllocationResponse> CreateAsync(CourseAllocationRequest request, Guid createdByUserId);

    /// <summary>
    /// Creates every row in one call — how Admin allocates a whole program's table (or a whole
    /// semester across programs) at once. All rows are validated up front; if any row fails
    /// validation, nothing is saved.
    /// </summary>
    Task<IEnumerable<CourseAllocationResponse>> CreateBulkAsync(CourseAllocationBulkRequest request, Guid createdByUserId);

    Task<CourseAllocationResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<CourseAllocationResponse>> GetAllAsync(CourseAllocationFilter filter);
    Task<PagedResponse<CourseAllocationResponse>> GetPagedAsync(int pageNumber, int pageSize, CourseAllocationFilter filter);
    Task<CourseAllocationResponse> UpdateAsync(Guid id, CourseAllocationRequest request);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Builds the compiled PDF for the given filter, in the standard multi-program allocation layout.
    /// AcademicYear/Semester on the filter drive the document's title (e.g. "2021/2022" + Second ->
    /// "SECOND SEMESTER COURSE ALLOCATION -2021/22").
    /// </summary>
    Task<byte[]> ExportPdfAsync(CourseAllocationFilter filter);
}
