using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;

namespace CompSci.Core.Services;

public class DissertationAllocationService : IDissertationAllocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public DissertationAllocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DissertationAllocationResponse> CreateAsync(DissertationAllocationRequest request, Guid createdByUserId)
    {
        var student = await ValidateStudentAsync(request.StudentId);
        await ValidateLecturerAsync(request.LecturerUserId);
        await EnsureNoExistingAllocationAsync(student.Id, request.AcademicYear);

        var allocation = new DissertationAllocation
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            LecturerUserId = request.LecturerUserId,
            AcademicYear = request.AcademicYear,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.DissertationAllocations.AddAsync(allocation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(allocation, student);
    }

    public async Task<DissertationAllocationResponse?> GetByIdAsync(Guid id)
    {
        var allocation = await _unitOfWork.DissertationAllocations.GetByIdAsync(id);
        return allocation == null ? null : await MapToResponseAsync(allocation);
    }

    public async Task<IEnumerable<DissertationAllocationResponse>> GetAllAsync(DissertationAllocationFilter filter)
    {
        var allocations = await FilterAsync(filter);
        return await MapToResponsesAsync(allocations);
    }

    public async Task<DissertationAllocationResponse> UpdateAsync(Guid id, DissertationAllocationRequest request)
    {
        var allocation = await _unitOfWork.DissertationAllocations.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Dissertation allocation with ID {id} not found.");

        var student = await ValidateStudentAsync(request.StudentId);
        await ValidateLecturerAsync(request.LecturerUserId);

        if (allocation.StudentId != student.Id || allocation.AcademicYear != request.AcademicYear)
            await EnsureNoExistingAllocationAsync(student.Id, request.AcademicYear);

        allocation.StudentId = student.Id;
        allocation.LecturerUserId = request.LecturerUserId;
        allocation.AcademicYear = request.AcademicYear;
        allocation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.DissertationAllocations.UpdateAsync(allocation);
        await _unitOfWork.SaveChangesAsync();

        return await MapToResponseAsync(allocation, student);
    }

    public async Task DeleteAsync(Guid id)
    {
        var allocation = await _unitOfWork.DissertationAllocations.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Dissertation allocation with ID {id} not found.");

        await _unitOfWork.DissertationAllocations.DeleteAsync(allocation);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<IEnumerable<DissertationAllocation>> FilterAsync(DissertationAllocationFilter filter)
    {
        Guid? studentGuid = null;
        if (!string.IsNullOrWhiteSpace(filter.StudentId))
        {
            var student = await _unitOfWork.Students.GetByStudentIdAsync(filter.StudentId);
            if (student == null)
                return Enumerable.Empty<DissertationAllocation>();

            studentGuid = student.Id;
        }

        var allocations = await _unitOfWork.DissertationAllocations.GetAllAsync();

        return allocations.Where(a =>
            (string.IsNullOrWhiteSpace(filter.AcademicYear) || a.AcademicYear == filter.AcademicYear) &&
            (filter.LecturerUserId == null || a.LecturerUserId == filter.LecturerUserId) &&
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

    private async Task ValidateLecturerAsync(Guid lecturerUserId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(lecturerUserId);
        if (user == null || user.Role != UserRole.Lecturer)
            throw new InvalidOperationException("LecturerUserId must reference an existing Lecturer account.");
    }

    private async Task EnsureNoExistingAllocationAsync(Guid studentId, string academicYear)
    {
        var existing = await _unitOfWork.DissertationAllocations.GetForStudentYearAsync(studentId, academicYear);
        if (existing != null)
            throw new InvalidOperationException("This student already has a dissertation supervisor allocated for that academic year.");
    }

    private async Task<DissertationAllocationResponse> MapToResponseAsync(DissertationAllocation allocation, Student? student = null)
    {
        student ??= await _unitOfWork.Students.GetByIdAsync(allocation.StudentId);
        var lecturer = await _unitOfWork.Users.GetByIdAsync(allocation.LecturerUserId);

        return MapToResponse(allocation, student, lecturer);
    }

    private async Task<IEnumerable<DissertationAllocationResponse>> MapToResponsesAsync(IEnumerable<DissertationAllocation> allocations)
    {
        var list = allocations.ToList();
        var students = await _unitOfWork.Students.GetAllAsync();
        var studentsById = students.ToDictionary(s => s.Id, s => s);
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        return list.Select(a =>
        {
            studentsById.TryGetValue(a.StudentId, out var student);
            usersById.TryGetValue(a.LecturerUserId, out var lecturer);
            return MapToResponse(a, student, lecturer);
        });
    }

    private static DissertationAllocationResponse MapToResponse(DissertationAllocation allocation, Student? student, User? lecturer)
    {
        return new DissertationAllocationResponse
        {
            Id = allocation.Id,
            StudentId = allocation.StudentId,
            StudentFullName = student == null ? string.Empty : $"{student.FirstName} {student.LastName}",
            StudentIdNumber = student?.StudentId ?? string.Empty,
            ProgramName = student?.ProgramName ?? string.Empty,
            LecturerUserId = allocation.LecturerUserId,
            LecturerUsername = lecturer?.Username,
            LecturerId = lecturer?.LecturerId,
            AcademicYear = allocation.AcademicYear,
            CreatedByUserId = allocation.CreatedByUserId,
            CreatedAt = allocation.CreatedAt,
            UpdatedAt = allocation.UpdatedAt
        };
    }
}
