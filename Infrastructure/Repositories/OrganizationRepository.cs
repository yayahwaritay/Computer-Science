using CompSci.Core.Entities;
using CompSci.Core.Interfaces;
using CompSci.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompSci.Infrastructure.Repositories;

public class OrganizationRepository : GenericRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(AppDbContext context) : base(context) { }

    public async Task<Organization?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.UserId == userId);
    }
}
