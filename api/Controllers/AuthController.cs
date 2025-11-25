using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using HealthCalendar.Shared;


// Most code here was taken from Demo-React-9-JWTAuthentication-Backend.pdf written by Baifan
namespace HealthCalendar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager,
                              IConfiguration configuration, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        // method for registering User with Patient Role
        [HttpPost("registerPatient")]
        public async Task<IActionResult> registerPatient([FromBody] RegisterDTO registerDTO)
        {
            try
            {
                // Create User with Role set to Patient
                var patient = new User
                {
                    Name = registerDTO.Name,
                    UserName = registerDTO.Email,
                    Role = Roles.Patient
                };

                // Attempts to create new user, automatically hashes passoword
                var result = await _userManager.CreateAsync(patient, registerDTO.Password);

                // For when registration succeeds
                if (result.Succeeded)
                {
                    _logger.LogInformation("[AuthController] Information from registerPatient(): \n " +
                                          $"Registration succeeded for Patient: {patient.Name}");
                    return Ok(new { Message = "Patient has been registered" });
                }

                // For when registration doesn't succeed
                _logger.LogWarning("[AuthController] Warning from RegisterPatient(): \n" +
                                  $"Registration failed for Patient: {patient.Name}");
                return BadRequest(result.Errors);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AuthController] Error from RegisterPatient(): \n" +
                                 "Something went wrong when registering Patient, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for logging in User
        [HttpPost("login")]
        public async Task<IActionResult> login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(loginDTO.Email);

                // Checks if login succeeds
                if (user != null && await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                {
                    _logger.LogInformation("[AuthController] Information from Login(): \n " +
                                          $"User {user.Name} was authorized");
                    // Generates and returns JWT Token
                    var token = generateJwtToken(user);
                    return Ok(new { Token = token });
                }
                
                // For when login doesn't succeed
                _logger.LogWarning("[AuthController] Warning from Login(): \n " +
                                   "User was unauthorized");
                return Unauthorized(new { Message = "User was unauthorized" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AuthController] Error from Login(): \n" +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for changing User's Password
        [Authorize]
        [HttpPost("changePassword")]
        public async Task<IActionResult> changePassword(ChangePasswordDTO changePasswordDTO)
        {
            try
            {
                var newPassword = changePasswordDTO.NewPassword;
                var newPasswordRepeated = changePasswordDTO.NewPasswordRepeated;
                if (newPassword != newPasswordRepeated)
                {
                    _logger.LogWarning("[AuthController] Warning from changePassword(): \n " +
                                       "Password change was unauthorized.");
                    return Unauthorized(new { Message = "Password change was unauthorized" });
                }

                // Retreives User with UserID in changePasswordDTO
                var userId = changePasswordDTO.UserId;
                var user = await _userManager.FindByIdAsync(changePasswordDTO.UserId);
                // In case User was not retreived
                if (user == null)
                {
                    _logger.LogError("[AuthController] Error from changePassword(): \n" +
                                     "Could not retreive User.");
                    return StatusCode(500, "Could not retreive User");
                }

                // Attempts to change the password
                var currentPassword = changePasswordDTO.CurrentPassword;
                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("[AuthController] Information from changePassword(): \n " +
                                          $"Password change succeeded for User: {user.Name}");
                    return Ok(new { Message = "Password was changed" });
                }
                
                // For when password change doesn't succeed
                _logger.LogWarning("[AuthController] Warning from changePassword(): \n " +
                                   "Password change was unauthorized.");
                return Unauthorized(new { Message = "Password change was unauthorized" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AuthController] Error from changePassword(): \n" +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("[AuthController] Information from Logout(): \n " +
                                       "Logout was successful");
                return Ok(new { Message = "Logout was successful" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AuthController] Error from Logout(): \n" +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for generating JWT token for user with Patient Role
        private string generateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"]; // Sectret key used for the signature
            if (string.IsNullOrEmpty(jwtKey))       // In case key is either null or empty
            {
                _logger.LogError("[AuthController] Error from GenerateJwtToken(): \n" +
                                 "JWT Key is missing from configuration.");
                throw new InvalidOperationException("JWT Key is missing from configuration.");
            }
            // Reads key from configuration
            var SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            // Uses HMAC SHA256 algorithm to sign the token
            var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),     // Token's subject (User's email)
                new Claim(JwtRegisteredClaimNames.Name, user.Name),         // User's Name
                new Claim(ClaimTypes.NameIdentifier, user.Id),              // User's unique Id
                new Claim(ClaimTypes.Role, user.Role),                      // User's Role
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Token's unique Id
                new Claim(JwtRegisteredClaimNames.Iat,
                          DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())    // Timestamp token was Issued at
            };
            // Related Worker's Id (For Patients only)
            if (user.Role == Roles.Patient)
            {
                var workerId = user.WorkerId ?? "-1"; // "-1" means Patient does not have related worker
                claims.Add(new Claim("WorkerId", workerId));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),  // Expiration time set to 1 hour (UTC)
                signingCredentials: credentials);   // Token is signed with specified credentials

            _logger.LogInformation("[AuthController] Information from GenerateJwtToken(): \n " +
                                  $"JWT Token was generated for User: {user.Name}");
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}