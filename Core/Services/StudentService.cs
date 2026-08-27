using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Services.Email;

namespace CompSci.Core.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public StudentService(IUnitOfWork unitOfWork, IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    public async Task<StudentResponse> CreateAsync(StudentRequest request)
    {
        if (await _unitOfWork.Students.StudentIdExistsAsync(request.StudentId))
            throw new InvalidOperationException($"Student with ID '{request.StudentId}' already exists.");

        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("A user with this email already exists.");

        if (await _unitOfWork.Users.UsernameExistsAsync(request.StudentId))
            throw new InvalidOperationException($"A user account already uses '{request.StudentId}' as a username.");

        var tempPassword = PasswordGenerator.GenerateTemporaryPassword();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.StudentId,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            Role = UserRole.Student,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Users.AddAsync(user);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
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

        var fullName = $"{student.FirstName} {student.LastName}";
        var (subject, html) = EmailTemplates.StudentWelcome(fullName, user.Email, tempPassword);
        await _emailSender.SendEmailAsync(user.Email, fullName, subject, html);

        return MapToResponse(student, user.Email);
    }

    public async Task<StudentResponse?> GetByIdAsync(Guid id)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
            return null;

        var user = await _unitOfWork.Users.GetByIdAsync(student.UserId);
        return MapToResponse(student, user?.Email ?? string.Empty);
    }

    public async Task<IEnumerable<StudentResponse>> GetAllAsync()
    {
        var students = await _unitOfWork.Students.GetAllAsync();
        var users = await _unitOfWork.Users.GetAllAsync();
        var emailsByUserId = users.ToDictionary(u => u.Id, u => u.Email);

        return students.Select(s => MapToResponse(s, emailsByUserId.GetValueOrDefault(s.UserId, string.Empty)));
    }

    public async Task<PagedResponse<StudentResponse>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var (data, totalCount) = await _unitOfWork.Students.GetPagedAsync(pageNumber, pageSize);
        var users = await _unitOfWork.Users.GetAllAsync();
        var emailsByUserId = users.ToDictionary(u => u.Id, u => u.Email);

        return new PagedResponse<StudentResponse>
        {
            Data = data.Select(s => MapToResponse(s, emailsByUserId.GetValueOrDefault(s.UserId, string.Empty))).ToList(),
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

        var user = await _unitOfWork.Users.GetByIdAsync(student.UserId);
        return MapToResponse(student, user?.Email ?? string.Empty);
    }

    public async Task DeleteAsync(Guid id)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Student with ID {id} not found.");

        await _unitOfWork.Students.DeleteAsync(student);

        var user = await _unitOfWork.Users.GetByIdAsync(student.UserId);
        if (user != null)
            await _unitOfWork.Users.DeleteAsync(user);

        await _unitOfWork.SaveChangesAsync();
    }

    private static StudentResponse MapToResponse(Student student, string email)
    {
        return new StudentResponse
        {
            Id = student.Id,
            Email = email,
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
