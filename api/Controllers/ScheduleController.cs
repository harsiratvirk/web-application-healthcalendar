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

        // method that retreives and Event's EventId 
        // The Event is from Schedule with given AvailabilityId
        [HttpGet("getScheduledEventId")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> getScheduledEventId([FromQuery] int availabilityId)
        {
            try
            {
                // retreives list of Schedules where AvailabilityId == availabilityId
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
                
                // EventId of first Schedule is retreived since schedules will only contain one distinct EventId
                var eventId = schedules.Select(s => s.EventId).FirstOrDefault();
                
                return Ok(eventId);
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

        // method that retreives and EventIds from list of Events
        // The Events are from Schedules with given AvailabilityId
        [HttpGet("getScheduledEventIds")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> getScheduledEventIds([FromQuery] int availabilityId)
        {
            try
            {
                // retreives list of Schedules where AvailabilityId == availabilityId
                var (schedules, status) = await _scheduleRepo
                    .getSchedulesByAvailabilityId(availabilityId);
                // In case getSchedulesByAvailabilityId() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from getScheduledEventIds(): \n" +
                                    "Could not retreive Schedules with getSchedulesByAvailabilityId() " + 
                                    "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }
                
                // retreives list of distinct eventIds from schedules
                var eventIds = schedules.Select(s => s.EventId).Distinct();
                
                return Ok(eventIds);
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
                
                // retreives EventId from schedule where Date == date from schedules if it exists
                var eventId = schedules
                    .Where(s => s.Date == date)
                    .Select(s => s.EventId)
                    .FirstOrDefault();

                return Ok(eventId);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[scheduleController] Error from findScheduledEventId(): \n" +
                                 "Something went wrong when trying to find an Event where " + 
                                $"DATE = {date} from Schedule with AvailabilityId = " +
                                $"{availabilityId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // HTTP POST functions

        // method that creates range of Schedules from given parameters and adds them into table
        [HttpPost("createSchedules")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> createSchedules([FromQuery] int[] availabilityIds, 
                                                         [FromQuery] int eventId, [FromQuery] DateOnly date)
        {
            try {
                // retreives Scheduled Event
                var (eventt, getEventStatus) = await _eventRepo.getEventById(eventId);
                // In case getEventById() did not succeed
                if (getEventStatus == OperationStatus.Error || eventt == null)
                {
                    _logger.LogError("[ScheduleController] Error from createSchedules(): \n" +
                                     "Could not retreive Event with getEventById() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Event");
                }

                // retreives Scheduled Availability
                var (availabilityRange, getAvailabilityStatus) = 
                    await _availabilityRepo.getAvailabilityByIds(availabilityIds);
                // In case getAvailabilityByIds() did not succeed
                if (getEventStatus == OperationStatus.Error || eventt == null)
                {
                    _logger.LogError("[ScheduleController] Error from createSchedules(): \n" +
                                     "Could not retreive range of Availability with " + 
                                     "getAvailabilityByIds() from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }
                
                // creates list of new Schedules and adds them into table
                var schedules = availabilityRange.Select(a => new Schedule
                {
                    Date = date,
                    AvailabilityId = a.AvailabilityId,
                    Availability = a,
                    EventId = eventId,
                    Event = eventt
                }).ToList();
                var status = await _scheduleRepo.createSchedules(schedules);

                // In case createSchedules() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from createSchedules(): \n" +
                                     "Could not create Schedules with createSchedules() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when creating Schedules");
                }
                return Ok(new { Message = "Schedules have been created" });

            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all availabilityIds
                var availabilityIdsString = String.Join(", ", availabilityIds);
                
                _logger.LogError("[ScheduleController] Error from createSchedules(): \n" +
                                 "Something went wrong when trying to create new Schedules " +
                                $"with Date = {date}, eventId = {eventId} and range of " + 
                                $"AvailabilityIds {availabilityIds}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }



        // HTTP PUT functions

        // method that updates Schedules by replacing old Availability with new Availability
        [HttpPut("updateScheduledAvailability")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            updateScheduledAvailability([FromQuery] int[] oldAvailabilityIds, 
                                        [FromQuery] int newAvailabilityId)
        {
            try
            {
                // retreives new Availability
                var (newAvailability, getAvailabilityStatus) = 
                    await _availabilityRepo.getAvailabilityById(newAvailabilityId);
                // In case getAvailabilityById() did not succeed
                if (getAvailabilityStatus == OperationStatus.Error || newAvailability == null)
                {
                    _logger.LogError("[ScheduleController] Error from updateScheduledAvailability(): \n" +
                                     "Could not retreive Availability with getAvailabilityById() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }

                // retreives list Schedules to be updated
                var (schedules, getSchedulesStatus) = 
                    await _scheduleRepo.getSchedulesByAvailabilityIds(oldAvailabilityIds);
                // In case getSchedulesByEventIds() did not succeed
                if (getSchedulesStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from updateScheduledAvailability(): \n" +
                                     "Could not retreive Schedules with getSchedulesByAvailabilityIds() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }

                // replaces all Availability properties in schedules with new Availability
                schedules.ForEach(s =>
                {
                    s.AvailabilityId = newAvailabilityId;
                    s.Availability = newAvailability;
                });

                // updates table with updated schedules
                var updateStatus = await _scheduleRepo.updateSchedules(schedules);
                // In case updateSchedules() did not succeed
                if (getSchedulesStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from updateScheduledAvailability(): \n" +
                                     "Could not update Schedules with updateSchedules() from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when updating Schedules");
                }

                return Ok(new { Message = "Schedules have been updated" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all oldAvailabilityIds
                var oldAvailabilityIdsString = String.Join(", ", oldAvailabilityIds);
                
                _logger.LogError("[scheduleController] Error from updateScheduledAvailability(): \n" +
                                 "Something went wrong when trying to update Schedules by replacing old " + 
                                $"Availability with AvailabilityIds {oldAvailabilityIdsString} with " +
                                $"new Availability with AvailabilityId = {newAvailabilityId}, " + 
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that updates Schedules by replacing old Event with updated Event
        [HttpPut("updateScheduledEvent")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> 
            updateScheduledEvent([FromQuery] int eventId, [FromQuery] int[] availabilityIds)
        {
            try
            {
                // retreives updated Event
                var (eventt, getEventStatus) = await _eventRepo.getEventById(eventId);
                // In case getEventById() did not succeed
                if (getEventStatus == OperationStatus.Error || eventt == null)
                {
                    _logger.LogError("[ScheduleController] Error from updateScheduledEvent(): \n" +
                                     "Could not retreive Availability with getEventById() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Event");
                }

                // retreives list of Schedules to be updated
                var (schedules, getSchedulesStatus) = 
                    await _scheduleRepo.getSchedulesAfterEventUpdate(eventId, availabilityIds);
                // In case getSchedulesAfterEventUpdate() did not succeed
                if (getSchedulesStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from updateScheduledEvent(): \n" +
                                     "Could not retreive Schedules with getSchedulesAfterEventUpdate() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }

                // replaces all Event properties in schedules with updated Event
                schedules.ForEach(s =>
                {
                    s.EventId = eventId;
                    s.Event = eventt;
                });

                // updates table with updated schedules
                var updateStatus = await _scheduleRepo.updateSchedules(schedules);
                // In case updateSchedules() did not succeed
                if (getSchedulesStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from updateScheduledEvent(): \n" +
                                     "Could not update Schedules with updateSchedules() from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when updating Schedules");
                }

                return Ok(new { Message = "Schedules have been updated" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all oldAvailabilityIds
                var availabilityIdsString = String.Join(", ", availabilityIds);
                
                _logger.LogError("[scheduleController] Error from updateScheduledAvailability(): \n" +
                                 "Something went wrong when trying to update Schedules where " + 
                                $"AvailabilityId is in {availabilityIdsString} by replacing old " + 
                                $"Event with updated Event new Event where EventId = {eventId}, " + 
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }



        // HTTP DELETE functions

        // method that deletes Schedules with given EventId from table
        [HttpDelete("deleteSchedulesByEventId")]
        [Authorize(Roles="Patient,Worker")]
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

        // method that deletes Schedules where their EventId is in givent list of EventIds from table
        [HttpDelete("deleteSchedulesByEventIds")]
        [Authorize(Roles="Worker,Usermanager")]
        public async Task<IActionResult> deleteSchedulesByEventIds([FromQuery] int[] eventIds)
        {
            try
            {
                // retreives list of Schedules to be deleted
                var (schedules, getStatus) = await _scheduleRepo.getSchedulesByEventIds(eventIds);
                // In case getSchedulesByEventIds() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from deleteSchedulesByEventIds(): \n" +
                                     "Could not retreive Schedules with getSchedulesByEventIds() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }

                // deletes schedules from table
                var deleteStatus = await _scheduleRepo.deleteSchedules(schedules);
                // In case deleteSchedules() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from deleteSchedulesByEventIds(): \n" +
                                     "Could not delete Schedules with deleteSchedules() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when deleting Schedules");
                }
                
                return Ok(new { Message = "Schedules have been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all EventIds
                var eventIdsString = String.Join(", ", eventIds);
                
                _logger.LogError("[ScheduleController] Error from deleteSchedulesByEventIds(): \n" +
                                 "Something went wrong when trying to delete range Schedules " +
                                $"with EventIds {eventIdsString}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that deletes necessary Schedules after an Event update
        [HttpDelete("deleteSchedulesAfterEventUpdate")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> 
            deleteSchedulesAfterEventUpdate([FromQuery] int eventId, [FromQuery] int[] availabilityIds)
        {
            try
            {
                // retreives list of Schedules to be deleted
                var (schedules, getStatus) = 
                    await _scheduleRepo.getSchedulesAfterEventUpdate(eventId, availabilityIds);
                // In case getSchedulesAfterEventUpdate() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from deleteSchedulesAfterEventUpdate(): \n" +
                                     "Could not retreive Schedules with getSchedulesAfterEventUpdate() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Schedules");
                }

                // deletes schedules from table
                var deleteStatus = await _scheduleRepo.deleteSchedules(schedules);
                // In case deleteSchedules() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[ScheduleController] Error from deleteSchedulesAfterEventUpdate(): \n" +
                                     "Could not delete Schedules with deleteSchedules() " + 
                                     "from ScheduleRepo.");
                    return StatusCode(500, "Something went wrong when deleting Schedules");
                }
                
                return Ok(new { Message = "Schedules have been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all AvailabilityIds
                var availabilityIdsString = String.Join(", ", availabilityIds);
                
                _logger.LogError("[ScheduleController] Error from deleteSchedulesAfterEventUpdate(): \n" +
                                 "Something went wrong when trying to delete range Schedules " +
                                $"with EventId {eventId} AvailabilityIds {availabilityIdsString}, " + 
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}