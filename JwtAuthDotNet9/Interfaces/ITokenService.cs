using JwtAuthDotNet9.Dtos.User;
using JwtAuthDotNet9.Entity;

namespace JwtAuthDotNet9.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(User user);
        string GenerateRefleshToken();
        string GenerateAndSaveRefleshToken(User user);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId);
        Task<TokenResponseDto?> RefleshTokenAsync(RefleshTokenDto refreshTokenDto);
    }
}
