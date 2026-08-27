using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserResponse> GetUserByIdAsync(Guid id);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task<StudentRegistrationResponse> RegisterStudentAsync(StudentSelfRegisterRequest request);
    Task<IEnumerable<PendingRegistrationResponse>> GetPendingRegistrationsAsync();
    Task<UserResponse> ApproveRegistrationAsync(Guid userId);
    Task RejectRegistrationAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
