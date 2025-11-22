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

        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(IScheduleRepo scheduleRepo, IAvailabilityRepo availabilityRepo, 
                                  IEventRepo eventRepo, ILogger<ScheduleController> logger)
        {
            _eventRepo = eventRepo;
            _scheduleRepo = scheduleRepo;
            _availabilityRepo = availabilityRepo;
            _logger = logger;
        }

        // HTTP GET functions

        // method that retreives finds Event's EventId 
        // The Event is from Schedule with given AvailabilityId
        [HttpGet("getScheduledEventId")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> getScheduledEventId(int availabilityId)
        {
            try
            {
                // retreives list of schedules where AvailabilityId == availabilityId
                var (schedules, status) = await _scheduleRepo
                    .getSchedulesByAvailabilityId(availabilityId);
                // In case getSchedulesByAvailabilityId() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from getScheduledEventId(): \n" +
                                    "Could not retreive Schedules with getSchedulesByAvailabilityId() " + 
                                    "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }
                
                // if schedules is empty, no Event is scheduled
                if (!schedules.Any()) return Ok(null);
                // EventId of first Schedule is returned because schedules will only contain one EventId
                return Ok(schedules.First().EventId);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[scheduleController] Error from getScheduledEventId(): \n" +
                                 "Something went wrong when trying to find an Event " + 
                                $"from Schedule with AvailabilityId = {availabilityId}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // method that finds Event's EventId 
        // The Event is from Schedule with given AvailabilityId where Date is same as given date
        [HttpGet("findScheduledEventId")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            findScheduledEventId([FromQuery] int availabilityId, [FromQuery] DateOnly date)
        {
            try
            {
                // retreives list of schedules where AvailabilityId == availabilityId
                var (schedules, status) = await _scheduleRepo
                    .getSchedulesByAvailabilityId(availabilityId);
                // In case getSchedulesByAvailabilityId() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from findScheduledEventId(): \n" +
                                    "Could not retreive Schedules with getSchedulesByAvailabilityId() " + 
                                    "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }
                
                // retreives Event where Date == date from schedules if it exists
                var eventt = schedules
                    .Select(s => s.Event)
                    .Where(e => e.Date == date)
                    .FirstOrDefault();

                if (eventt == null) return Ok(null);
                return Ok(eventt.EventId);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[scheduleController] Error from findScheduledEventId(): \n" +
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