using CompSci.Core.Entities;

namespace CompSci.Core.Interfaces;

public interface IOrganizationRepository : IGenericRepository<Organization>
{
    Task<Organization?> GetByUserIdAsync(Guid userId);
}
