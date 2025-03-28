using System.Security.Claims;
using JwtAuthDotNet9.Dtos.User;
using JwtAuthDotNet9.Entities;
using JwtAuthDotNet9.Interfaces;
using JwtAuthDotNet9.Models;
using JwtAuthDotNet9.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthDotNet9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;

        public AuthController(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
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

                var existingUser = await _authRepository.GetUserByEmailAsync(request.Email!);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User Already Exists" });
                }

                var createdUser = await _authRepository.RegisterAsync(request);
                if (createdUser == null)
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during registration", stackTrace = ex.StackTrace });
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

                var user = await _authRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return NotFound(new { message = "User Not Found" });
                }

                var authToken = await _authRepository.LoginAsync(request);
                if (authToken == null)
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                return Ok(new { message = "Login Successful", token = authToken });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"Error during login: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }
    }
}
