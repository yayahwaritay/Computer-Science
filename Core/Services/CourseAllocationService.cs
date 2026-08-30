using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;

namespace CompSci.Core.Services;

public class CourseAllocationService : ICourseAllocationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICourseAllocationPdfBuilder _pdfBuilder;

    public CourseAllocationService(IUnitOfWork unitOfWork, ICourseAllocationPdfBuilder pdfBuilder)
    {
        _unitOfWork = unitOfWork;
        _pdfBuilder = pdfBuilder;
    }

    public async Task<CourseAllocationResponse> CreateAsync(CourseAllocationRequest request, Guid createdByUserId)
    {
        await ValidateLecturerLinkAsync(request);

        var allocation = MapToEntity(request, createdByUserId);
        await _unitOfWork.CourseAllocations.AddAsync(allocation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(allocation);
    }

    public async Task<IEnumerable<CourseAllocationResponse>> CreateBulkAsync(CourseAllocationBulkRequest request, Guid createdByUserId)
    {
        if (request.Allocations.Count == 0)
            throw new InvalidOperationException("At least one allocation row is required.");

        var errors = new List<string>();
        for (var i = 0; i < request.Allocations.Count; i++)
        {
            var rowErrors = CourseAllocationValidator.Validate(request.Allocations[i]);
            errors.AddRange(rowErrors.Select(e => $"Row {i + 1}: {e}"));
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        foreach (var row in request.Allocations)
            await ValidateLecturerLinkAsync(row);

        var allocations = request.Allocations.Select(row => MapToEntity(row, createdByUserId)).ToList();
        foreach (var allocation in allocations)
            await _unitOfWork.CourseAllocations.AddAsync(allocation);

        await _unitOfWork.SaveChangesAsync();

        return await MapToResponsesAsync(allocations);
    }

    public async Task<CourseAllocationResponse?> GetByIdAsync(Guid id)
    {
        var allocation = await _unitOfWork.CourseAllocations.GetByIdAsync(id);
        return allocation == null ? null : await MapToResponseAsync(allocation);
    }

    public async Task<IEnumerable<CourseAllocationResponse>> GetAllAsync(CourseAllocationFilter filter)
    {
        var allocations = await FilterAsync(filter);
        return await MapToResponsesAsync(allocations);
    }

    public async Task<PagedResponse<CourseAllocationResponse>> GetPagedAsync(int pageNumber, int pageSize, CourseAllocationFilter filter)
    {
        var filtered = (await FilterAsync(filter)).ToList();
        var totalCount = filtered.Count;

        var page = filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<CourseAllocationResponse>
        {
            Data = (await MapToResponsesAsync(page)).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<CourseAllocationResponse> UpdateAsync(Guid id, CourseAllocationRequest request)
    {
        var allocation = await _unitOfWork.CourseAllocations.GetByIdAsync(id);
        if (allocation == null)
            throw new KeyNotFoundException($"Course allocation with ID {id} not found.");

        await ValidateLecturerLinkAsync(request);

        allocation.AcademicYear = request.AcademicYear;
        allocation.Semester = request.Semester;
        allocation.ProgramName = request.ProgramName;
        allocation.YearOfStudy = request.YearOfStudy;
        allocation.CourseCode = request.CourseCode;
        allocation.CourseDescription = request.CourseDescription;
        allocation.CreditHours = request.CreditHours;
        allocation.StaffName = request.StaffName;
        allocation.LecturerUserId = request.LecturerUserId;
        allocation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CourseAllocations.UpdateAsync(allocation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(allocation);
    }

    public async Task DeleteAsync(Guid id)
    {
        var allocation = await _unitOfWork.CourseAllocations.GetByIdAsync(id);
        if (allocation == null)
            throw new KeyNotFoundException($"Course allocation with ID {id} not found.");

        await _unitOfWork.CourseAllocations.DeleteAsync(allocation);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<byte[]> ExportPdfAsync(CourseAllocationFilter filter)
    {
        var allocations = await FilterAsync(filter);
        var responses = await MapToResponsesAsync(allocations);

        var academicYear = filter.AcademicYear ?? string.Empty;
        var semester = filter.Semester ?? Semester.Second;

        return _pdfBuilder.Build(academicYear, semester, responses);
    }

    private async Task<IEnumerable<CourseAllocation>> FilterAsync(CourseAllocationFilter filter)
    {
        var allocations = await _unitOfWork.CourseAllocations.GetAllAsync();

        return allocations.Where(a =>
            (string.IsNullOrWhiteSpace(filter.AcademicYear) || a.AcademicYear == filter.AcademicYear) &&
            (filter.Semester == null || a.Semester == filter.Semester) &&
            (string.IsNullOrWhiteSpace(filter.ProgramName) || a.ProgramName.Contains(filter.ProgramName, StringComparison.OrdinalIgnoreCase)) &&
            (filter.LecturerUserId == null || a.LecturerUserId == filter.LecturerUserId));
    }

    /// <summary>
    /// If a LecturerUserId is supplied, it must reference an existing account with the Lecturer role.
    /// </summary>
    private async Task ValidateLecturerLinkAsync(CourseAllocationRequest request)
    {
        if (request.LecturerUserId == null)
            return;

        var user = await _unitOfWork.Users.GetByIdAsync(request.LecturerUserId.Value);
        if (user == null || user.Role != UserRole.Lecturer)
            throw new InvalidOperationException("LecturerUserId must reference an existing Lecturer account.");
    }

    private static CourseAllocation MapToEntity(CourseAllocationRequest request, Guid createdByUserId)
    {
        return new CourseAllocation
        {
            Id = Guid.NewGuid(),
            AcademicYear = request.AcademicYear,
            Semester = request.Semester,
            ProgramName = request.ProgramName,
            YearOfStudy = request.YearOfStudy,
            CourseCode = request.CourseCode,
            CourseDescription = request.CourseDescription,
            CreditHours = request.CreditHours,
            StaffName = request.StaffName,
            LecturerUserId = request.LecturerUserId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<CourseAllocationResponse> MapToResponseAsync(CourseAllocation allocation)
    {
        User? lecturer = allocation.LecturerUserId.HasValue
            ? await _unitOfWork.Users.GetByIdAsync(allocation.LecturerUserId.Value)
            : null;

        return MapToResponse(allocation, lecturer);
    }

    private async Task<IEnumerable<CourseAllocationResponse>> MapToResponsesAsync(IEnumerable<CourseAllocation> allocations)
    {
        var list = allocations.ToList();
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        return list.Select(a =>
        {
            User? lecturer = null;
            if (a.LecturerUserId.HasValue)
                usersById.TryGetValue(a.LecturerUserId.Value, out lecturer);

            return MapToResponse(a, lecturer);
        });
    }

    private static CourseAllocationResponse MapToResponse(CourseAllocation allocation, User? lecturer)
    {
        return new CourseAllocationResponse
        {
            Id = allocation.Id,
            AcademicYear = allocation.AcademicYear,
            Semester = allocation.Semester,
            SemesterText = allocation.Semester.ToString(),
            ProgramName = allocation.ProgramName,
            YearOfStudy = allocation.YearOfStudy,
            CourseCode = allocation.CourseCode,
            CourseDescription = allocation.CourseDescription,
            CreditHours = allocation.CreditHours,
            StaffName = allocation.StaffName,
            LecturerUserId = allocation.LecturerUserId,
            LecturerUsername = lecturer?.Username,
            LecturerId = lecturer?.LecturerId,
            CreatedByUserId = allocation.CreatedByUserId,
            CreatedAt = allocation.CreatedAt,
            UpdatedAt = allocation.UpdatedAt
        };
    }
}
