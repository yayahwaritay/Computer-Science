using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;

namespace CompSci.Core.Services;

public class InternshipAllocationService : IInternshipAllocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public InternshipAllocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<InternshipAllocationResponse> CreateAsync(InternshipAllocationRequest request, Guid createdByUserId)
    {
        var student = await ValidateStudentAsync(request.StudentId);
        var organization = await ValidateOrganizationAsync(request.OrganizationId);
        await ValidateLecturerAsync(request.LecturerUserId);
        await EnsureNoExistingAllocationAsync(student.Id, request.AcademicYear, request.Semester);

        var allocation = new InternshipAllocation
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            OrganizationUserId = organization.UserId,
            LecturerUserId = request.LecturerUserId,
            AcademicYear = request.AcademicYear,
            Semester = request.Semester,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.InternshipAllocations.AddAsync(allocation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(allocation, student, organization);
    }

    public async Task<InternshipAllocationResponse?> GetByIdAsync(Guid id)
    {
        var allocation = await _unitOfWork.InternshipAllocations.GetByIdAsync(id);
        return allocation == null ? null : await MapToResponseAsync(allocation);
    }

    public async Task<IEnumerable<InternshipAllocationResponse>> GetAllAsync(InternshipAllocationFilter filter)
    {
        var allocations = await FilterAsync(filter);
        return await MapToResponsesAsync(allocations);
    }

    public async Task<InternshipAllocationResponse> UpdateAsync(Guid id, InternshipAllocationRequest request)
    {
        var allocation = await _unitOfWork.InternshipAllocations.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Internship allocation with ID {id} not found.");

        var student = await ValidateStudentAsync(request.StudentId);
        var organization = await ValidateOrganizationAsync(request.OrganizationId);
        await ValidateLecturerAsync(request.LecturerUserId);

        if (allocation.StudentId != student.Id || allocation.AcademicYear != request.AcademicYear || allocation.Semester != request.Semester)
            await EnsureNoExistingAllocationAsync(student.Id, request.AcademicYear, request.Semester);

        allocation.StudentId = student.Id;
        allocation.OrganizationUserId = organization.UserId;
        allocation.LecturerUserId = request.LecturerUserId;
        allocation.AcademicYear = request.AcademicYear;
        allocation.Semester = request.Semester;
        allocation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.InternshipAllocations.UpdateAsync(allocation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(allocation, student, organization);
    }

    public async Task DeleteAsync(Guid id)
    {
        var allocation = await _unitOfWork.InternshipAllocations.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Internship allocation with ID {id} not found.");

        await _unitOfWork.InternshipAllocations.DeleteAsync(allocation);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<IEnumerable<InternshipAllocation>> FilterAsync(InternshipAllocationFilter filter)
    {
        Guid? studentGuid = null;
        if (!string.IsNullOrWhiteSpace(filter.StudentId))
        {
            var student = await _unitOfWork.Students.GetByStudentIdAsync(filter.StudentId);
            if (student == null)
                return Enumerable.Empty<InternshipAllocation>();

            studentGuid = student.Id;
        }

        var allocations = await _unitOfWork.InternshipAllocations.GetAllAsync();

        return allocations.Where(a =>
            (string.IsNullOrWhiteSpace(filter.AcademicYear) || a.AcademicYear == filter.AcademicYear) &&
            (filter.Semester == null || a.Semester == filter.Semester) &&
            (filter.LecturerUserId == null || a.LecturerUserId == filter.LecturerUserId) &&
            (filter.OrganizationUserId == null || a.OrganizationUserId == filter.OrganizationUserId) &&
            (studentGuid == null || a.StudentId == studentGuid));
    }

    /// <summary>Resolves the human-readable Student ID (e.g. "24807") to the linked Student record.</summary>
    private async Task<Student> ValidateStudentAsync(string studentId)
    {
        var student = await _unitOfWork.Students.GetByStudentIdAsync(studentId);
        if (student == null)
            throw new InvalidOperationException($"No student found with Student ID '{studentId}'.");

        return student;
    }

    private async Task<Organization> ValidateOrganizationAsync(Guid organizationId)
    {
        var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
        if (organization == null)
            throw new InvalidOperationException("OrganizationId must reference an existing organization.");

        return organization;
    }

    private async Task ValidateLecturerAsync(Guid lecturerUserId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(lecturerUserId);
        if (user == null || user.Role != UserRole.Lecturer)
            throw new InvalidOperationException("LecturerUserId must reference an existing Lecturer account.");
    }

    private async Task EnsureNoExistingAllocationAsync(Guid studentId, string academicYear, Semester semester)
    {
        var existing = await _unitOfWork.InternshipAllocations.GetForStudentPeriodAsync(studentId, academicYear, semester);
        if (existing != null)
            throw new InvalidOperationException("This student already has an internship allocation for that academic year and semester.");
    }

    private async Task<InternshipAllocationResponse> MapToResponseAsync(InternshipAllocation allocation, Student? student = null, Organization? organization = null)
    {
        student ??= await _unitOfWork.Students.GetByIdAsync(allocation.StudentId);
        organization ??= await _unitOfWork.Organizations.GetByUserIdAsync(allocation.OrganizationUserId);
        var lecturer = await _unitOfWork.Users.GetByIdAsync(allocation.LecturerUserId);

        return MapToResponse(allocation, student, organization, lecturer);
    }

    private async Task<IEnumerable<InternshipAllocationResponse>> MapToResponsesAsync(IEnumerable<InternshipAllocation> allocations)
    {
        var list = allocations.ToList();
        var students = await _unitOfWork.Students.GetAllAsync();
        var studentsById = students.ToDictionary(s => s.Id, s => s);
        var organizations = await _unitOfWork.Organizations.GetAllAsync();
        var organizationsByUserId = organizations.ToDictionary(o => o.UserId, o => o);
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        return list.Select(a =>
        {
            studentsById.TryGetValue(a.StudentId, out var student);
            organizationsByUserId.TryGetValue(a.OrganizationUserId, out var organization);
            usersById.TryGetValue(a.LecturerUserId, out var lecturer);
            return MapToResponse(a, student, organization, lecturer);
        });
    }

    private static InternshipAllocationResponse MapToResponse(InternshipAllocation allocation, Student? student, Organization? organization, User? lecturer)
    {
        return new InternshipAllocationResponse
        {
            Id = allocation.Id,
            StudentId = allocation.StudentId,
            StudentFullName = student == null ? string.Empty : $"{student.FirstName} {student.LastName}",
            StudentIdNumber = student?.StudentId ?? string.Empty,
            ProgramName = student?.ProgramName ?? string.Empty,
            OrganizationId = organization?.Id ?? Guid.Empty,
            OrganizationUserId = allocation.OrganizationUserId,
            OrganizationName = organization?.Name,
            LecturerUserId = allocation.LecturerUserId,
            LecturerUsername = lecturer?.Username,
            LecturerId = lecturer?.LecturerId,
            AcademicYear = allocation.AcademicYear,
            Semester = allocation.Semester,
            SemesterText = allocation.Semester.ToString(),
            CreatedByUserId = allocation.CreatedByUserId,
            CreatedAt = allocation.CreatedAt,
            UpdatedAt = allocation.UpdatedAt
        };
    }
}
