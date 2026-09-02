namespace CompSci.Core.DTOs;

public class OrganizationRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;
}

public class OrganizationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? CredentialsExpireAt { get; set; }
    public bool CredentialsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
}
