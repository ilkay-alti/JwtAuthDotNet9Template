using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthDotNet9.Dtos.User;
using JwtAuthDotNet9.Entity;

namespace JwtAuthDotNet9.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> RegisterAsync(RegisterDto request);
        Task<string?> LoginAsync(LoginDto request);

        Task<string> GenerateAndSaveRefleshToken(User user);
    }
}