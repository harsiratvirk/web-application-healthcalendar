using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using HealthCalendar.DAL;



namespace HealthCalendar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepo _authRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthRepo authRepo, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _authRepo = authRepo;
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

                // Attempts to create new user
                var (status, errors) = await _authRepo.registerUser(patient, registerDTO.Password);

                // For when registration doesn't succeed
                if (status == OperationStatus.Error)
                {
                    // For when registration doesn't succeed
                    _logger.LogWarning("[AuthController] Warning from RegisterPatient(): \n" +
                                       "registerUser from AuthRepo could not register " + 
                                      $"Patient: {patient.Name}");
                    return BadRequest(errors);
                }
                // For when registration succeeds
                _logger.LogInformation("[AuthController] Information from registerPatient(): \n " +
                                          $"Registration succeeded for Patient: {patient.Name}");
                return Ok(new { Message = "Patient has been registered" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AuthController] Error from RegisterPatient(): \n" +
                                 "Something went wrong when registering Patient, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for registering User with Worker Role
        [Authorize(Roles="Admin")]
        [HttpPost("registerWorker")]
        public async Task<IActionResult> registerWorker([FromBody] RegisterDTO registerDTO)
        {
            try
            {
                // Create User with Role set to Patient
                var worker = new User
                {
                    Name = registerDTO.Name,
                    UserName = registerDTO.Email,
                    Role = Roles.Worker
                };

                // Attempts to create new user
                var (status, errors) = await _authRepo.registerUser(worker, registerDTO.Password);

                // For when registration doesn't succeed
                if (status == OperationStatus.Error)
                {
                    // For when registration doesn't succeed
                    _logger.LogWarning("[AuthController] Warning from RegisterWorker(): \n" +
                                       "registerUser from AuthRepo could not register " + 
                                      $"Worker: {worker.Name}");
                    return BadRequest(errors);
                }
                // For when registration succeeds
                _logger.LogInformation("[AuthController] Information from RegisterWorker(): \n " +
                                      $"Registration succeeded for Worker: {worker.Name}");
                return Ok(new { Message = "Worker has been registered" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AuthController] Error from RegisterWorker(): \n" +
                                 "Something went wrong when registering Worker, " +
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
                // retreives User
                var (user, getStatus) = await _authRepo.getUserByUsername(loginDTO.Email);
                // In case of server error
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AuthController] Error from login(): \n" +
                                     "Could not retreive User with getUserByUsername() " + 
                                    $"from AuthRepo");
                    return StatusCode(500, "Internal server error");
                }
                // In case user was not retreived
                if (user == null)
                {
                    _logger.LogWarning("[AuthController] Warning from Login(): \n " +
                                       "User was unauthorized");
                    return Unauthorized(new { Message = "User was unauthorized" });
                }

                // checks User's password
                var checkStatus = await _authRepo.checkPassword(user, loginDTO.Password);
                // In case of server error
                if (checkStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AuthController] Error from login(): \n" +
                                     "Could not check password with checkPassword() " + 
                                    $"from AuthRepo");
                    return StatusCode(500, "Internal server error");
                }
                // In case user was not retreived
                if (checkStatus == OperationStatus.Unauthorized)
                {
                    _logger.LogWarning("[AuthController] Warning from Login(): \n " +
                                       "User was unauthorized");
                    return Unauthorized(new { Message = "User was unauthorized" });
                }

                _logger.LogInformation("[AuthController] Information from Login(): \n " +
                                      $"User {user.Name} was authorized");
                // Generates and returns JWT Token
                var token = generateJwtToken(user);
                return Ok(new { Token = token });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AuthController] Error from Login(): \n" +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // backend logout method, is not necessary but clears eventual auth cookies from server
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> logout()
        {
            try
            {
                var status = await _authRepo.logout();
                // In case of server error
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[AuthController] Error from logout(): \n" +
                                     "Could not log out User with logout() " + 
                                    $"from AuthRepo");
                    return StatusCode(500, "Internal server error");
                }

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