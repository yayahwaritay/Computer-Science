using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Interfaces;

namespace CompSci.Core.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CourseResponse> CreateAsync(CourseRequest request)
    {
        if (await _unitOfWork.Courses.CourseCodeExistsAsync(request.CourseCode))
            throw new InvalidOperationException($"Course with code '{request.CourseCode}' already exists.");

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseCode = request.CourseCode,
            CourseName = request.CourseName,
            CreditHour = request.CreditHour,
            Staff = request.Staff,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Courses.AddAsync(course);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(course);
    }

    public async Task<CourseResponse?> GetByIdAsync(Guid id)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id);
        return course == null ? null : MapToResponse(course);
    }

    public async Task<IEnumerable<CourseResponse>> GetAllAsync()
    {
        var courses = await _unitOfWork.Courses.GetAllAsync();
        return courses.Select(MapToResponse);
    }

    public async Task<PagedResponse<CourseResponse>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var (data, totalCount) = await _unitOfWork.Courses.GetPagedAsync(pageNumber, pageSize);

        return new PagedResponse<CourseResponse>
        {
            Data = data.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<CourseResponse> UpdateAsync(Guid id, CourseRequest request)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Course with ID {id} not found.");

        var existingByCode = await _unitOfWork.Courses.GetByCourseCodeAsync(request.CourseCode);
        if (existingByCode != null && existingByCode.Id != id)
            throw new InvalidOperationException($"Course with code '{request.CourseCode}' already exists.");

        course.CourseCode = request.CourseCode;
        course.CourseName = request.CourseName;
        course.CreditHour = request.CreditHour;
        course.Staff = request.Staff;
        course.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Courses.UpdateAsync(course);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(course);
    }

    public async Task DeleteAsync(Guid id)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Course with ID {id} not found.");

        await _unitOfWork.Courses.DeleteAsync(course);
        await _unitOfWork.SaveChangesAsync();
    }

    private static CourseResponse MapToResponse(Course course)
    {
        return new CourseResponse
        {
            Id = course.Id,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            CreditHour = course.CreditHour,
            Staff = course.Staff,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
