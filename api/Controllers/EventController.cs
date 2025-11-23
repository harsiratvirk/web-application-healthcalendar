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
        
        // userManager used to retreive Users related to Event upon creation
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EventController> _logger;

        public EventController(IEventRepo eventRepo, UserManager<User> userManager, 
                               ILogger<EventController> logger)
        {
            _eventRepo = eventRepo;
            _userManager = userManager;
            _logger = logger;
        }

        // HTTP GET functions

        // method for retreiving Patient's Events for the week
        [HttpGet("getWeeksEventsForPatient")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> 
            getWeeksEventsForPatient([FromQuery] string userId, [FromQuery] DateOnly monday)
        {
            try {
                // retreives list of Patient's Events
                var sunday = monday.AddDays(6);
                var (weeksEvents, status) = 
                        await _eventRepo.getWeeksEventsForPatient(userId, monday, sunday);
                // In case getWeeksEventsForPatient() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from getWeeksEventsForWorker(): \n" +
                                         "Could not retreive Events with getWeeksEventsForPatient() " + 
                                         "from EventRepo.");
                        return StatusCode(500, "Something went wrong when retreiving Events for the week");
                }

                // makes list of EventDTOs from weeksEvents
                var weeksEventDTOs = weeksEvents.Select(e => new EventDTO
                {
                    EventId = e.EventId,
                    From = e.From,
                    To = e.To,
                    Date = e.Date,
                    Title = e.Title,
                    Location = e.Title,
                    UserId = userId
                });

                return Ok(weeksEventDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[EventController] Error from getWeeksEventsForPatient(): \n" +
                                 "Something went wrong when trying to retreive week's events where " + 
                                $"UserId == {userId} and monday is on the date {monday}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // method for retreiving Events of Worker's Patients for the week
        [HttpGet("getWeeksEventsForWorker")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> getWeeksEventsForWorker([FromBody] List<UserDTO> patients, 
                                                                 [FromQuery] DateOnly monday)
        {
            try {
                var weeksEventsWithOwners = new List<EventWithOwnerDTO>();
                var sunday = monday.AddDays(6);

                foreach (var patient in patients)
                {
                    // retreives list of Events for Patient
                    var userId = patient.Id;
                    var (events, status) = 
                        await _eventRepo.getWeeksEventsForPatient(userId, monday, sunday);
                    // In case getWeeksEventsForPatient() did not succeed
                    if (status == OperationStatus.Error)
                    {
                        _logger.LogError("[EventController] Error from " + 
                                         "getWeeksEventsForWorker(): \n" +
                                         "Could not retreive Events with " + 
                                         "getWeeksEventsForPatient() from EventRepo.");
                        return StatusCode(500, "Something went wrong when retreiving " + 
                                               "Events for the week");
                    }

                    // makes list of EventWithOwnerDTOs from events
                    var eventsWithOwners = events.Select(e => new EventWithOwnerDTO
                    {
                        EventId = e.EventId,
                        From = e.From,
                        To = e.To,
                        Date = e.Date,
                        Title = e.Title,
                        Location = e.Title,
                        UserId = userId,
                        OwnerName = patient.Name
                    });

                    weeksEventsWithOwners.AddRange(eventsWithOwners);
                }

                return Ok(weeksEventsWithOwners);
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all patients
                var patientStrings = patients.ConvertAll(u => $"{@u}");
                var patientsString = String.Join(", ", patientStrings);
                
                _logger.LogError("[EventController] Error from getWeeksEventsForWorker(): \n" +
                                 "Something went wrong when trying to retreive week's events for " + 
                                $"Patients {patientsString} where monday is on the date {monday}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
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

        // method that deletes range of Events from table with list of EventIds
        [HttpDelete("deleteEventsByIds")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> deleteEventsByIds([FromQuery] int[] eventIds)
        {
            try
            {
                // retreives range of Events that should be deleted
                var (events, getStatus) = 
                    await _eventRepo.getEventsByIds(eventIds);
                // In case getEventsByIds() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from deleteEventsByIds(): \n" +
                                     "Could not retreive Events with getEventByIds() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Events");
                }

                // deletes events from table
                var deleteStatus = await _eventRepo.deleteEvents(events);
                // In case deleteEvents() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from deleteEventsByIds(): \n" +
                                     "Could not delete Events with deleteEvents() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when deleting Availability");
                }
                
                return Ok(new { Message = "Events have been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all EventIds
                var eventIdsString = String.Join(", ", eventIds);

                _logger.LogError("[EventController] Error from deleteEventsByIds(): \n" +
                                 "Something went wrong when trying to delete range of Events " +
                                $"with EventIds {eventIdsString}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}