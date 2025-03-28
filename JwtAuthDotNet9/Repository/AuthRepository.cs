using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthDotNet9.Data;
using JwtAuthDotNet9.Dtos.User;
using JwtAuthDotNet9.Entities;
using JwtAuthDotNet9.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using JwtAuthDotNet9.Interfaces;

namespace JwtAuthDotNet9.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbCntex _dbContext;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            ApplicationDbCntex dbContext,
            IConfiguration configuration,
            ITokenService tokenService,
            ILogger<AuthRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordHasher = new PasswordHasher<User>();
        }

        /// <summary>
        /// Retrieves a user by their email address
        /// </summary>
        /// <param name="email">The email address to search for</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        /// <summary>
        /// Retrieves a user by their ID
        /// </summary>
        /// <param name="id">The user ID to search for</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <summary>
        /// Retrieves a user by their username
        /// </summary>
        /// <param name="username">The username to search for</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == username);
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token if authentication succeeds, null otherwise</returns>
        public async Task<string?> LoginAsync(LoginDto request)
        {
            try
            {
                var user = await GetUserByEmailAsync(request.Email);

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

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">Registration information</param>
        /// <returns>The created user if successful</returns>
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
                var existingUser = await GetUserByEmailAsync(request.Email!);
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

                // Add user to database
                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User registered successfully: {Username}, {Email}", user.Username, user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", request.Email);
                throw;
            }
        }
    }
}