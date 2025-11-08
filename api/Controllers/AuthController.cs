using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.VisualBasic;

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
        public async Task<IActionResult> RegisterPatient([FromBody] RegisterDTO registerDTO)
        {
            try
            {
                // Create User with Role set to Patient
                var patient = new User
                {
                    Name = registerDTO.Name,
                    Email = registerDTO.Email,
                    Role = Role.Patient
                };

                // Attempts to create new user, automatically hashes passoword
                var result = await _userManager.CreateAsync(patient, registerDTO.Password);

                // if-statement in case registration fails
                if (!result.Succeeded)
                {
                    _logger.LogWarning("[AuthController] Warning from RegisterPatient(): \n" +
                                      $"Registration failed for Patient: {patient.Name}");
                    return BadRequest(result.Errors);
                }

                _logger.LogInformation("[AuthController] Information from RegisterPatient(): \n " +
                                      $"Registration succeeded for Patient: {patient.Name}");
                return Ok(new { Message = "Patient has been registered" });
            }
            catch (Exception e)
            {
                _logger.LogError("[AuthController] Error from RegisterPatient(): \n" +
                                 "Something went wrong when registrating Patient, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        // method for generating JWT token
    }
}