using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthCalendar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(UserManager<User> userManager, ILogger<UserController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // HTTP GET functions

        // Retrieves User with given Id
        [HttpGet("getUser/{userId}")]
        [Authorize]
        public async Task<IActionResult> getUser(string userId)
        {
            try
            {
                // retreives User with Id
                var user = await _userManager.FindByIdAsync(userId);
                // in case User was not retreived
                if (user == null)
                {
                    _logger.LogError("[UserController] Error from getUser(): \n" +
                                     "Could not retreive User");
                    return StatusCode(500, "Could not retreive User");
                }

                var userDTO = new UserDTO
                {
                    Id = userId,
                    UserName = user.UserName!,
                    Name = user.Name,
                    Role = user.Role,
                    WorkerId = user.WorkerId
                };
                return Ok(userDTO);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getUser(): \n" +
                                 "Something went wrong when trying to retreive User " + 
                                $"where Id = {userId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        // Retrieves Users related to Worker with given Id
        [HttpGet("getUsersByWorkerId")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> getUsersByWorkerId([FromQuery] string workerId)
        {
            try
            {
                // retreives list of Users with "Patient" role
                // converts list into UserDTOs
                var userDTOs = await _userManager.Users
                    .Where(u => u.WorkerId == workerId)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Name = u.Name,
                        Role = u.Role,
                        WorkerId = u.WorkerId
                    }).ToListAsync();
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getIdsByWorkerId(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where WorkerId = {workerId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


    }
}