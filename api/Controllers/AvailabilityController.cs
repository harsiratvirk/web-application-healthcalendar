using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HealthCalendar.DAL;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvailabilityController : ControllerBase
    {
        private readonly IAvailabilityRepo _availabilityRepo;
        
        // userManager used to retreive Users related to Availability upon creation
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AvailabilityController(IAvailabilityRepo availabilityRepo, UserManager<User> userManager, 
                                      ILogger<AuthController> logger)
        {
            _availabilityRepo = availabilityRepo;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("createAvailability")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> createAvailability(AvailabilityDTO availabilityDTO)
        {
            try {
                // retreives Worker related to new Availability
                var userId = availabilityDTO.UserId;
                var worker = await _userManager.FindByIdAsync(userId);
                
                // creates new Availability using availabilityDTO and worker
                var availability = new Availability
                {
                    AvailabilityId = availabilityDTO.AvailabilityId,
                    From = availabilityDTO.From,
                    To = availabilityDTO.To,
                    DayOfWeek = availabilityDTO.DayOfWeek,
                    Date = availabilityDTO.Date,
                    UserId = userId,
                    Worker = worker!
                };
                var status = await _availabilityRepo.createAvailability(availability);

                // In case createAvailability() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from createAvailability(): \n" +
                                     "Something went wrong when calling createAvailability() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when creating Availability");
                }
                return Ok(new { Message = "Availability has been created" });

            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from createAvailability(): \n" +
                                 "Something went wrong when trying to create new Availability, " +
                                $"with AvailabilityDTO {@availabilityDTO} Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
