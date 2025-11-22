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

        // HTTP GET functions

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


        // HTTP DELETE functions

        // method that deletes Schedules with given EventId from table
        [HttpDelete("deleteSchedulesByEventId")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> deleteSchedulesByEventId([FromQuery] int eventId)
        {
            try
            {
                // retreives list of Schedules to be deleted
                var (schedules, getStatus) = await _scheduleRepo.getSchedulesByEventId(eventId);
                // In case getSchedulesByEventId() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from deleteSchedulesByEventId(): \n" +
                                     "Could not retreive Schedules with getSchedulesByEventId() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }

                // deletes schedules from table
                var deleteStatus = await _scheduleRepo.deleteSchedules(schedules);
                // In case deleteSchedules() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from deleteSchedulesByEventId(): \n" +
                                     "Could not delete Schedules with deleteSchedules() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when deleting Schedules");
                }
                
                return Ok(new { Message = "Schedules have been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[ScheduleController] Error from deleteSchedulesByEventId(): \n" +
                                 "Something went wrong when trying to delete Schedules " +
                                $"where EventId = {eventId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}