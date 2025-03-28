using JwtAuthDotNet9.Dtos.User;
using JwtAuthDotNet9.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthDotNet9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;

        public AuthController(IAuthRepository authRepository, ITokenService tokenService, IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _authRepository = authRepository;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid input data", errors = ModelState });
                }

                var existingUser = await _userRepository.GetUserByEmailAsync(request.Email!);
                if (existingUser is not null)
                {
                    return BadRequest(new { message = "User Already Exists" });
                }

                var createdUser = await _authRepository.RegisterAsync(request);
                if (createdUser is null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "User Creation Failed" });
                }

                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = "User Created",
                    user = new
                    {
                        id = createdUser.Id,
                        username = createdUser.Username,
                        email = createdUser.Email,
                        role = createdUser.Role
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Database error occurred during registration",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred during registration",
                    error = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid input data", errors = ModelState });
                }

                var user = await _userRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return NotFound(new { message = "User Not Found" });
                }

                var authToken = await _authRepository.LoginAsync(request);
                if (authToken == null)
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                var refreshToken = await _authRepository.GenerateAndSaveRefleshToken(user);

                var responseToken = new TokenResponseDto
                {
                    AccessToken = authToken,
                    RefreshToken = refreshToken,
                };

                return Ok(new { message = "Login Successful", responseToken });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"Error during login: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }


        [HttpPost("reflesh-token")]
        public async Task<IActionResult> RefleshToken([FromBody] RefleshTokenDto request)
        {
            try
            {
                var result = await _tokenService.RefleshTokenAsync(request);
                if (result is null || result.AccessToken is null || result.RefreshToken is null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new { message = "Invalid refresh token" });
                }

                return StatusCode(StatusCodes.Status200OK, new TokenResponseDto
                {
                    AccessToken = result.AccessToken,
                    RefreshToken = result.RefreshToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"Error during token refresh: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("TestAuthorization")]
        [Authorize]
        public IActionResult TestAuthantication()
        {
            return StatusCode(StatusCodes.Status200OK, new { message = "Authenticated" });
        }

        [HttpGet("TestAuthorizationWithRoleAdmin")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult TestAuthorizationWithRole()
        {
            return StatusCode(StatusCodes.Status200OK, new { message = "Authenticated with ADMIN role" });
        }
    }
}
