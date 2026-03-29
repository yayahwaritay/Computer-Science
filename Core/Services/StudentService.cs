using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Interfaces;

namespace CompSci.Core.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StudentResponse> CreateAsync(StudentRequest request)
    {
        if (await _unitOfWork.Students.StudentIdExistsAsync(request.StudentId))
            throw new InvalidOperationException($"Student with ID '{request.StudentId}' already exists.");

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            StudentId = request.StudentId,
            ProgramName = request.ProgramName,
            Year = request.Year,
            EnrollmentYear = request.EnrollmentYear,
            ExpectedGraduation = request.ExpectedGraduation,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Students.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(student);
    }

    public async Task<StudentResponse?> GetByIdAsync(Guid id)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        return student == null ? null : MapToResponse(student);
    }

    public async Task<IEnumerable<StudentResponse>> GetAllAsync()
    {
        var students = await _unitOfWork.Students.GetAllAsync();
        return students.Select(MapToResponse);
    }

    public async Task<PagedResponse<StudentResponse>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var (data, totalCount) = await _unitOfWork.Students.GetPagedAsync(pageNumber, pageSize);

        return new PagedResponse<StudentResponse>
        {
            Data = data.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<StudentResponse> UpdateAsync(Guid id, StudentRequest request)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Student with ID {id} not found.");

        var existingByStudentId = await _unitOfWork.Students.GetByStudentIdAsync(request.StudentId);
        if (existingByStudentId != null && existingByStudentId.Id != id)
            throw new InvalidOperationException($"Student with ID '{request.StudentId}' already exists.");

        student.FirstName = request.FirstName;
        student.LastName = request.LastName;
        student.StudentId = request.StudentId;
        student.ProgramName = request.ProgramName;
        student.Year = request.Year;
        student.EnrollmentYear = request.EnrollmentYear;
        student.ExpectedGraduation = request.ExpectedGraduation;
        student.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Students.UpdateAsync(student);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(student);
    }

    public async Task DeleteAsync(Guid id)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Student with ID {id} not found.");

        await _unitOfWork.Students.DeleteAsync(student);
        await _unitOfWork.SaveChangesAsync();
    }

    private static StudentResponse MapToResponse(Student student)
    {
        return new StudentResponse
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            FullName = $"{student.FirstName} {student.LastName}",
            StudentId = student.StudentId,
            ProgramName = student.ProgramName,
            Year = student.Year,
            EnrollmentYear = student.EnrollmentYear,
            ExpectedGraduation = student.ExpectedGraduation,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
    }
}
