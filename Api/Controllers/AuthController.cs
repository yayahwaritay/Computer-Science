using System.Security.Claims;
using CompSci.Core.DTOs;
using CompSci.Core.Interfaces;
using CompSci.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompSci.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var errors = AuthValidator.ValidateRegister(request);
        if (errors.Any())
            return BadRequest(ApiResponse<AuthResponse>.FailResponse("Validation failed.", errors));

        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), ApiResponse<AuthResponse>.SuccessResponse(result, "User registered successfully."));
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var errors = AuthValidator.ValidateLogin(request);
        if (errors.Any())
            return BadRequest(ApiResponse<AuthResponse>.FailResponse("Validation failed.", errors));

        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.SuccessResponse(result, "Login successful."));
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _authService.GetUserByIdAsync(id);
        return Ok(ApiResponse<UserResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _authService.GetAllUsersAsync();
        return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Self-register as a student. The account is created in a pending state and must be
    /// approved by an Admin or Lecturer before it can be used to log in.
    /// </summary>
    [HttpPost("register-student")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterStudent([FromBody] StudentSelfRegisterRequest request)
    {
        var errors = AuthValidator.ValidateStudentSelfRegister(request);
        if (errors.Any())
            return BadRequest(ApiResponse<StudentRegistrationResponse>.FailResponse("Validation failed.", errors));

        var result = await _authService.RegisterStudentAsync(request);
        return CreatedAtAction(nameof(RegisterStudent), ApiResponse<StudentRegistrationResponse>.SuccessResponse(result, "Registration received and pending approval."));
    }

    /// <summary>
    /// List student registrations awaiting approval (Admin/Lecturer only)
    /// </summary>
    [HttpGet("pending-registrations")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> GetPendingRegistrations()
    {
        var result = await _authService.GetPendingRegistrationsAsync();
        return Ok(ApiResponse<IEnumerable<PendingRegistrationResponse>>.SuccessResponse(result));
    }

    /// <summary>
    /// Approve a pending student registration (Admin/Lecturer only)
    /// </summary>
    [HttpPost("{userId}/approve")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> ApproveRegistration(Guid userId)
    {
        var result = await _authService.ApproveRegistrationAsync(userId);
        return Ok(ApiResponse<UserResponse>.SuccessResponse(result, "Registration approved."));
    }

    /// <summary>
    /// Reject a pending student registration (Admin/Lecturer only)
    /// </summary>
    [HttpPost("{userId}/reject")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> RejectRegistration(Guid userId)
    {
        await _authService.RejectRegistrationAsync(userId);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Registration rejected."));
    }

    /// <summary>
    /// Change the current user's password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var errors = AuthValidator.ValidateChangePassword(request);
        if (errors.Any())
            return BadRequest(ApiResponse<bool>.FailResponse("Validation failed.", errors));

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _authService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Password changed successfully."));
    }
}
