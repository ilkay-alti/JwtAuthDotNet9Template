using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtAuthDotNet9.Dtos.User;
using JwtAuthDotNet9.Entity;
using JwtAuthDotNet9.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuthDotNet9.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        private readonly IUserRepository _userRepository;

        public TokenService(IConfiguration config, IUserRepository userRepository)
        {
            _config = config; // Assign _config first
            var signingKey = _config["JWT:SigningKey"] ?? throw new ArgumentNullException("JWT:SigningKey", "Signing key cannot be null.");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public string CreateToken(User user)
        {
            var now = DateTime.UtcNow; // Always use UTC for token timestamps

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
                // Add any additional claims as needed
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                IssuedAt = now,
                NotBefore = now, // Token is valid starting from now
                Expires = now.AddDays(1), // Expires 1 day from now (or whatever your desired expiration is)
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public string GenerateAndSaveRefleshToken(User user)
        {
            var refreshToken = GenerateRefleshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            return refreshToken;
        }

        public string GenerateRefleshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        public async Task<TokenResponseDto?> RefleshTokenAsync(RefleshTokenDto refreshTokenDto)
        {
            var isValid = await ValidateRefreshTokenAsync(refreshTokenDto.RefleshToken!, refreshTokenDto.UserId);
            if (!isValid)
                return null;

            var user = await _userRepository.GetUserByIdAsync(refreshTokenDto.UserId);
            if (user == null)
                return null;

            var accessToken = CreateToken(user);
            var refreshToken = GenerateAndSaveRefleshToken(user);

            // Update user with new refresh token
            await _userRepository.UpdateUserAsync(user);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }



    }

}