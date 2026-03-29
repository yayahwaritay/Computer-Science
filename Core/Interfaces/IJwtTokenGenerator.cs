namespace CompSci.Core.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string username, string role);
}
