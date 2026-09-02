using CompSci.Core.DTOs;

namespace CompSci.Core.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationResponse> RegisterAsync(OrganizationRegisterRequest request);
    Task<OrganizationResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<OrganizationResponse>> GetAllAsync();

    /// <summary>
    /// Generates a new default password and a fresh 2-week expiry window for an organization
    /// whose credentials have lapsed (or need to be handed out again), and emails it to them.
    /// </summary>
    Task<OrganizationResponse> ReissueCredentialsAsync(Guid id);

    Task DeleteAsync(Guid id);
}
