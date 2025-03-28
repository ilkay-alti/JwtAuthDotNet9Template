using JwtAuthDotNet9.Entities;

namespace JwtAuthDotNet9.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
