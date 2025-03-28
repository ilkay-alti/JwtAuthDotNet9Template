using JwtAuthDotNet9.Entity;

namespace JwtAuthDotNet9.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task UpdateUserAsync(User user);


    }
}
