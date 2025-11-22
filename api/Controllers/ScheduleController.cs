using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthCalendar.DAL;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleController : ControllerBase
    {
        
        private readonly IScheduleRepo _scheduleRepo;

        // availabilityRepo used to retreive Availability related to Schedule for creation and uptades
        private readonly IAvailabilityRepo _availabilityRepo;
        
        // eventRepo used to retreive Event related to Schedule for creation and uptades
        private readonly IEventRepo _eventRepo;

        private readonly ILogger<AuthController> _logger;

        public ScheduleController(IScheduleRepo scheduleRepo, IAvailabilityRepo availabilityRepo, 
                                  IEventRepo eventRepo, ILogger<AuthController> logger)
        {
            _eventRepo = eventRepo;
            _scheduleRepo = scheduleRepo;
            _availabilityRepo = availabilityRepo;
            _logger = logger;
        }

        // method that finds Event set upon given Date from Schedules with given AvailabilityId
        [HttpGet("findScheduledEvent")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            findScheduledEvent([FromQuery] int availabilityId, [FromQuery] DateOnly date)
        {
            try
            {
                // retreives list of schedules where AvailabilityId == availabilityId
                var (schedules, status) = await _scheduleRepo
                    .getSchedulesByAvailabilityId(availabilityId);
                // In case getSchedulesByAvailabilityId() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from findScheduledEvent(): \n" +
                                    "Could not retreive Schedules with getSchedulesByAvailabilityId() " + 
                                    "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }
                
                // retreives Event where Date == date from schedules if it exists
                var eventt = schedules
                    .Select(s => s.Event)
                    .Where(e => e.Date == date)
                    .FirstOrDefault();
                return Ok(eventt);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[scheduleController] Error from findScheduledEvent(): \n" +
                                 "Something went wrong when trying to find an Event wher " + 
                                $"DATE = {date} from Schedule with AvailabilityId = " +
                                $"{availabilityId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}