using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthDotNet9.Dtos.User;
using JwtAuthDotNet9.Entities;

namespace JwtAuthDotNet9.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> RegisterAsync(RegisterDto request);
        Task<string?> LoginAsync(LoginDto request);

        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByUsernameAsync(string username);
    }
}