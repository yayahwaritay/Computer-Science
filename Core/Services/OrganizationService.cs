using System.Security.Cryptography;
using CompSci.Core.DTOs;
using CompSci.Core.Entities;
using CompSci.Core.Enums;
using CompSci.Core.Interfaces;
using CompSci.Core.Services.Email;

namespace CompSci.Core.Services;

public class OrganizationService : IOrganizationService
{
    private const int CredentialValidityDays = 14;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public OrganizationService(IUnitOfWork unitOfWork, IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    public async Task<OrganizationResponse> RegisterAsync(OrganizationRegisterRequest request)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("A user with this email already exists.");

        var username = await GenerateUsernameAsync(request.Name);
        var expiresAt = DateTime.UtcNow.AddDays(CredentialValidityDays);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.DefaultPassword),
            Role = UserRole.Organization,
            IsApproved = true,
            CredentialsExpireAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Users.AddAsync(user);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Organizations.AddAsync(organization);

        await _unitOfWork.SaveChangesAsync();

        var (subject, html) = EmailTemplates.OrganizationCredentialsIssued(organization.Name, user.Email, request.DefaultPassword, expiresAt);
        await _emailSender.SendEmailAsync(user.Email, organization.Name, subject, html);

        return MapToResponse(organization, user);
    }

    public async Task<OrganizationResponse?> GetByIdAsync(Guid id)
    {
        var organization = await _unitOfWork.Organizations.GetByIdAsync(id);
        if (organization == null)
            return null;

        var user = await _unitOfWork.Users.GetByIdAsync(organization.UserId);
        return user == null ? null : MapToResponse(organization, user);
    }

    public async Task<IEnumerable<OrganizationResponse>> GetAllAsync()
    {
        var organizations = (await _unitOfWork.Organizations.GetAllAsync()).ToList();
        var users = await _unitOfWork.Users.GetAllAsync();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        return organizations
            .Where(o => usersById.ContainsKey(o.UserId))
            .Select(o => MapToResponse(o, usersById[o.UserId]));
    }

    public async Task<OrganizationResponse> ReissueCredentialsAsync(Guid id)
    {
        var organization = await _unitOfWork.Organizations.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Organization with ID {id} not found.");

        var user = await _unitOfWork.Users.GetByIdAsync(organization.UserId)
            ?? throw new KeyNotFoundException($"Organization with ID {id} not found.");

        var newPassword = GenerateDefaultPassword();
        var expiresAt = DateTime.UtcNow.AddDays(CredentialValidityDays);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.CredentialsExpireAt = expiresAt;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var (subject, html) = EmailTemplates.OrganizationCredentialsIssued(organization.Name, user.Email, newPassword, expiresAt);
        await _emailSender.SendEmailAsync(user.Email, organization.Name, subject, html);

        return MapToResponse(organization, user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var organization = await _unitOfWork.Organizations.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Organization with ID {id} not found.");

        var user = await _unitOfWork.Users.GetByIdAsync(organization.UserId);

        await _unitOfWork.Organizations.DeleteAsync(organization);
        if (user != null)
            await _unitOfWork.Users.DeleteAsync(user);

        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Slugifies the organization name into a unique username, e.g. "Acme Ltd" -> "acmeltd", "acmeltd2".</summary>
    private async Task<string> GenerateUsernameAsync(string organizationName)
    {
        var baseSlug = new string(organizationName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "org";
        if (baseSlug.Length > 40)
            baseSlug = baseSlug[..40];

        var candidate = baseSlug;
        var suffix = 1;
        while (await _unitOfWork.Users.UsernameExistsAsync(candidate))
        {
            suffix++;
            candidate = $"{baseSlug}{suffix}";
        }

        return candidate;
    }

    private static string GenerateDefaultPassword()
    {
        // Guarantees the AuthValidator/OrganizationValidator complexity rule (upper+lower+digit, 8+ chars).
        var randomPart = Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
        return $"Org{randomPart}9";
    }

    private static OrganizationResponse MapToResponse(Organization organization, User user)
    {
        return new OrganizationResponse
        {
            Id = organization.Id,
            UserId = user.Id,
            Name = organization.Name,
            Email = user.Email,
            CredentialsExpireAt = user.CredentialsExpireAt,
            CredentialsExpired = user.CredentialsExpireAt.HasValue && user.CredentialsExpireAt.Value < DateTime.UtcNow,
            CreatedAt = organization.CreatedAt
        };
    }
}
