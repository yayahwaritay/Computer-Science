using CompSci.Core.Enums;

namespace CompSci.Core.DTOs;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>Unique Lecturer identifier (e.g. "LEC-0001"). Null/empty for non-Lecturer accounts.</summary>
    public string? LecturerId { get; set; }

    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? LecturerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StudentSelfRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int EnrollmentYear { get; set; }
    public int ExpectedGraduation { get; set; }
}

public class StudentRegistrationResponse
{
    public Guid UserId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = "Registration received. An administrator or lecturer must approve your account before you can log in.";
}

public class PendingRegistrationResponse
{
    public Guid UserId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
