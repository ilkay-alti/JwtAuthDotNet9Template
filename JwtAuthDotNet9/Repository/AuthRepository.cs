using JwtAuthDotNet9.Data;
using JwtAuthDotNet9.Dtos.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JwtAuthDotNet9.Interfaces;
using JwtAuthDotNet9.Entity;

namespace JwtAuthDotNet9.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            ApplicationDbContext dbContext,
            IConfiguration configuration,
            ITokenService tokenService,
            IUserRepository userRepository,
            ILogger<AuthRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<string?> LoginAsync(LoginDto request)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
                    return null;
                }

                var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
                    return null;
                }

                // Generate JWT token
                var token = _tokenService.CreateToken(user);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process for {Email}", request.Email);
                throw;
            }
        }

        public async Task<User?> RegisterAsync(RegisterDto request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.UserName) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Registration attempt with invalid data");
                    return null;
                }

                // Check if user with the same email already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(request.Email!);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                    throw new InvalidOperationException("User with this email already exists.");
                }

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.UserName!,
                    Email = request.Email!,
                    Role = "USER",
                };

                // Hash password
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password!);

                // Add to database
                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("New user registered successfully: {Email}", request.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}: {Message}",
                    request.Email, ex.Message);
                throw;
            }
        }

        public async Task<string> GenerateAndSaveRefleshToken(User user)
        {
            var refreshToken = _tokenService.GenerateAndSaveRefleshToken(user);
            await _userRepository.UpdateUserAsync(user);
            return refreshToken;
        }
    }
}