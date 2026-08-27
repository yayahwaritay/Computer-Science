using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Services.Email;

namespace CompSci.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailSender _emailSender;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailSender = emailSender;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("A user with this email already exists.");

        if (await _unitOfWork.Users.UsernameExistsAsync(request.Username))
            throw new InvalidOperationException("A user with this username already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            LecturerId = request.Role == UserRole.Lecturer ? await GenerateLecturerIdAsync() : null,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Username, user.Role.ToString());

        return new AuthResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            LecturerId = user.LecturerId,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddHours(24)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.Role == UserRole.Student && !user.IsApproved)
            throw new UnauthorizedAccessException("Your registration is pending approval by an administrator or lecturer.");

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Username, user.Role.ToString());

        return new AuthResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            LecturerId = user.LecturerId,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddHours(24)
        };
    }

    /// <summary>
    /// Generates the next sequential, unique Lecturer identifier (e.g. "LEC-0001").
    /// </summary>
    private async Task<string> GenerateLecturerIdAsync()
    {
        var nextNumber = await _unitOfWork.Users.CountByRoleAsync(UserRole.Lecturer) + 1;
        return $"LEC-{nextNumber:D4}";
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        return MapToResponse(user);
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<StudentRegistrationResponse> RegisterStudentAsync(StudentSelfRegisterRequest request)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("A user with this email already exists.");

        if (await _unitOfWork.Users.UsernameExistsAsync(request.Username))
            throw new InvalidOperationException("A user with this username already exists.");

        if (await _unitOfWork.Students.StudentIdExistsAsync(request.StudentId))
            throw new InvalidOperationException($"Student with ID '{request.StudentId}' already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Student,
            IsApproved = false,
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
        var (subject, html) = EmailTemplates.RegistrationReceived(fullName);
        await _emailSender.SendEmailAsync(user.Email, fullName, subject, html);

        return new StudentRegistrationResponse
        {
            UserId = user.Id,
            StudentProfileId = student.Id,
            Email = user.Email
        };
    }

    public async Task<IEnumerable<PendingRegistrationResponse>> GetPendingRegistrationsAsync()
    {
        var pendingUsers = await _unitOfWork.Users.GetPendingStudentApprovalsAsync();

        var responses = new List<PendingRegistrationResponse>();
        foreach (var user in pendingUsers)
        {
            var student = await _unitOfWork.Students.GetByUserIdAsync(user.Id);
            responses.Add(new PendingRegistrationResponse
            {
                UserId = user.Id,
                StudentProfileId = student?.Id ?? Guid.Empty,
                Username = user.Username,
                Email = user.Email,
                FullName = student == null ? string.Empty : $"{student.FirstName} {student.LastName}",
                StudentId = student?.StudentId ?? string.Empty,
                ProgramName = student?.ProgramName ?? string.Empty,
                CreatedAt = user.CreatedAt
            });
        }

        return responses;
    }

    public async Task<UserResponse> ApproveRegistrationAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        if (user.Role != UserRole.Student)
            throw new InvalidOperationException("Only student registrations require approval.");

        if (user.IsApproved)
            throw new InvalidOperationException("This registration has already been approved.");

        user.IsApproved = true;
        user.ApprovedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var student = await _unitOfWork.Students.GetByUserIdAsync(user.Id);
        var fullName = student == null ? user.Username : $"{student.FirstName} {student.LastName}";
        var (subject, html) = EmailTemplates.RegistrationApproved(fullName);
        await _emailSender.SendEmailAsync(user.Email, fullName, subject, html);

        return MapToResponse(user);
    }

    public async Task RejectRegistrationAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        if (user.Role != UserRole.Student || user.IsApproved)
            throw new InvalidOperationException("Only pending student registrations can be rejected.");

        var student = await _unitOfWork.Students.GetByUserIdAsync(user.Id);
        var fullName = student == null ? user.Username : $"{student.FirstName} {student.LastName}";

        if (student != null)
            await _unitOfWork.Students.DeleteAsync(student);
        await _unitOfWork.Users.DeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var (subject, html) = EmailTemplates.RegistrationRejected(fullName);
        await _emailSender.SendEmailAsync(user.Email, fullName, subject, html);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            LecturerId = user.LecturerId,
            CreatedAt = user.CreatedAt
        };
    }
}
