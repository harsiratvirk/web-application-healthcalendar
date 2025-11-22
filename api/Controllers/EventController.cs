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
    public class EventController : ControllerBase
    {
        private readonly IEventRepo _eventRepo;
        
        // userManager used to retreive Users related to Availability upon creation
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EventController> _logger;

        public EventController(IEventRepo eventRepo, UserManager<User> userManager, 
                               ILogger<EventController> logger)
        {
            _eventRepo = eventRepo;
            _userManager = userManager;
            _logger = logger;
        }

        // HTTP DELETE functions

        // method that deletes Event from table
        [HttpDelete("deleteEvent/{eventId}")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> deleteEvent(int eventId)
        {
            try
            {
                // retreives Event that should be deleted
                var (eventt, getStatus) = await _eventRepo.getEventById(eventId);
                // In case getEventById() did not succeed
                if (getStatus == OperationStatus.Error || eventt == null)
                {
                    _logger.LogError("[EventController] Error from deleteEvent(): \n" +
                                     "Could not retreive Event with getEventById() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Event");
                }

                // deletes eventt from table
                var deleteStatus = await _eventRepo.deleteEvent(eventt);
                // In case deleteAvailability() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from deleteEvent(): \n" +
                                     "Could not delete Event with deleteEvent() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when deleting Event");
                }
                
                return Ok(new { Message = "Event has been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[EventController] Error from deleteEvent(): \n" +
                                 "Something went wrong when trying to delete Event " +
                                $"with EventId = {eventId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}