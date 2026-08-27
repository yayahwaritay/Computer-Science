using CompSci.Core.Entities;
using CompSci.Core.Enums;

namespace CompSci.Core.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<IEnumerable<User>> GetPendingStudentApprovalsAsync();
    Task<int> CountByRoleAsync(UserRole role);
}
