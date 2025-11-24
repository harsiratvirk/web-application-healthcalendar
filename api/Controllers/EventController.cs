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

        // method for retreiving Event by its EventId
        [HttpGet("getEvent/{eventId}")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> getEvent(int eventId)
        {
            try
            {
                var (eventt, status) = await _eventRepo.getEventById(eventId);
                // In case getEventById() did not succeed
                if (status == OperationStatus.Error || eventt == null)
                {
                    _logger.LogError("[EventController] Error from getEvent(): \n" +
                                         "Could not retreive Event with getEventById() from EventRepo.");
                        return StatusCode(500, "Something went wrong when retreiving Events for the week");
                }

                var eventDTO = new EventDTO
                {
                    EventId = eventId,
                    From = eventt.From,
                    To = eventt.To,
                    Date = eventt.Date,
                    Title = eventt.Title,
                    Location = eventt.Title,
                    UserId = eventt.UserId
                };
                return Ok(eventDTO);
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[EventController] Error from getEvent(): \n" +
                                 "Something went wrong when trying to retreive Event with " + 
                                $"EventId == {eventId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

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
                var patientStrings = patients.ConvertAll(e => $"{@e}");
                var patientsString = String.Join(", ", patientStrings);
                
                _logger.LogError("[EventController] Error from getWeeksEventsForWorker(): \n" +
                                 "Something went wrong when trying to retreive week's events for " + 
                                $"Patients {patientsString} where monday is on the date {monday}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that validates Event for a Create Event method
        [HttpGet("validateEventForCreate")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> validateEventForCreate([FromBody] EventDTO eventDTO)
        {
            try
            {
                var (datesEvents, getStatus) = 
                    await _eventRepo.getEventsByDate(eventDTO.UserId, eventDTO.Date);
                // In case getEventsByDate() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from validateEventForCreate(): \n" +
                                     "Could not retreive Events with getEventsByDate() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when retreiving " + 
                                           "Events for the date");
                }
                
                // validates eventDTO and checks if it overlaps with any Event in datesEvents
                var validationStatus = validateEvent(eventDTO, datesEvents);
                // In case something went wrong in validateEvent()
                if (validationStatus == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from validateEventForCreate(): \n" +
                                     "Could not validate Event with validateEvent() " + 
                                     "from EventController.");
                    return StatusCode(500, "Something went wrong when validating Event");
                }
                // In case eventDTO was Not acceptable, status code for Not Acceptable is returned
                if (validationStatus == OperationStatus.NotAcceptable) 
                    return StatusCode(406, "Event was found Not Acceptable");
                
                return Ok(new { Message = "Event was found Acceptable" });
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[EventController] Error from validateEventForCreate(): \n" +
                                 "Something went wrong when trying to validate eventDTO " + 
                                $"{@eventDTO}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that validates Event for an Update Event method
        [HttpGet("validateEventForUpdate")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> validateEventForUpdate([FromBody] EventDTO eventDTO)
        {
            try
            {
                var (datesEvents, getStatus) = 
                    await _eventRepo.getEventsByDate(eventDTO.UserId, eventDTO.Date);
                // In case getEventsByDate() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from validateEventForUpdate(): \n" +
                                     "Could not retreive Events with getEventsByDate() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when retreiving " + 
                                           "Events for the date");
                }

                // removes Event that will be updated from list
                var eventId = eventDTO.EventId;
                datesEvents.Where(e => e.EventId != eventId);
                
                // validates eventDTO and checks if it overlaps with any Event in datesEvents
                var validationStatus = validateEvent(eventDTO, datesEvents);
                // In case something went wrong in validateEvent()
                if (validationStatus == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from validateEventForUpdate(): \n" +
                                     "Could not validate Event with validateEvent() " + 
                                     "from EventController.");
                    return StatusCode(500, "Something went wrong when validating Event");
                }
                // In case eventDTO was Not acceptable, status code for Not Acceptable is returned
                if (validationStatus == OperationStatus.NotAcceptable) 
                    return StatusCode(406, "Event was found Not Acceptable");
                
                return Ok(new { Message = "Event was found Acceptable" });
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[EventController] Error from validateEventForUpdate(): \n" +
                                 "Something went wrong when trying to validate eventDTO " + 
                                $"{@eventDTO}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // HTTP POST functions

        // method that adds new Event into table
        [HttpPost("createEvent")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> createEvent([FromBody] EventDTO eventDTO)
        {
            try {
                // retreives Worker and adds it into availabilityDTO
                var userId = eventDTO.UserId;
                var patient = await _userManager.FindByIdAsync(userId);
                
                // creates new Event using eventDTO and patient
                var eventt = new Event
                {
                    From = eventDTO.From,
                    To = eventDTO.To,
                    Date = eventDTO.Date,
                    Title = eventDTO.Title,
                    Location = eventDTO.Location,
                    UserId = userId,
                    Patient = patient!
                };
                var status = await _eventRepo.createEvent(eventt);

                // In case createEvent() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from createEvent(): \n" +
                                     "Could not create Event with createEvent() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when creating Event");
                }
                return Ok(new { Message = "Event has been created" });

            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[EventController] Error from createEvent(): \n" +
                                 "Something went wrong when trying to create new Event " +
                                $"with eventDTO {@eventDTO}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // HTTP PUT functions
        [HttpPost("updateEvent")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> updateEvent([FromBody] EventDTO eventDTO)
        {
            try {
                // retreives Worker and adds it into availabilityDTO
                var userId = eventDTO.UserId;
                var patient = await _userManager.FindByIdAsync(userId);
                
                // updates new Event using eventDTO and patient
                var eventt = new Event
                {
                    EventId = eventDTO.EventId,
                    From = eventDTO.From,
                    To = eventDTO.To,
                    Date = eventDTO.Date,
                    Title = eventDTO.Title,
                    Location = eventDTO.Location,
                    UserId = userId,
                    Patient = patient!
                };
                var status = await _eventRepo.updateEvent(eventt);

                // In case updateEvent() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[EventController] Error from updatedEvent(): \n" +
                                     "Could not update Event with updateEvent() " + 
                                     "from EventRepo.");
                    return StatusCode(500, "Something went wrong when updating Event");
                }
                return Ok(new { Message = "Event has been updated" });

            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[EventController] Error from updateEvent(): \n" +
                                 "Something went wrong when trying to update Event " +
                                $"with eventDTO {@eventDTO}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        
        // HTTP DELETE functions

        // method that deletes Event from table
        [HttpDelete("deleteEvent/{eventId}")]
        [Authorize(Roles="Patient,Worker")]
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


        // PRIVATE functions

        // method that checks if given EventDTO is valid and doesn't overlap with other Events
        private OperationStatus validateEvent(EventDTO eventDTO, List<Event> datesEvents)
        {
            try
            {
                var from = eventDTO.From;
                var to = eventDTO.To;
                var date = eventDTO.Date;
                // calculates factor of difference between to and from, divided by 30 min
                // timeDifference must be divisible by 30 min for Event to be valid
                var timeDifference = to.ToTimeSpan() - from.ToTimeSpan();
                var timeDifferenceFactor = timeDifference.Divide(TimeSpan.FromMinutes(30));
                // Checks to see if Event is invalid
                if (from >= to || date < DateOnly.FromDateTime(DateTime.Today) || 
                    timeDifferenceFactor != Math.Round(timeDifferenceFactor))
                {
                    _logger.LogInformation("[EventController] Information from " + 
                                           "validateEvent(): \n" +
                                          $"Event {@eventDTO} was not acceptable.");
                    return OperationStatus.NotAcceptable;
                }

                // Iterates through datesEvents and looks for overlap with eventDTO
                foreach (var eventt in datesEvents)
                {
                    // checks if eventDTO and eventt overlaps
                    if (from < eventt.To && eventt.From < to)
                    {
                        _logger.LogInformation("[EventController] Information from " + 
                                               "validateEvent(): \n" +
                                              $"Event {@eventDTO} was not acceptable.");
                        return OperationStatus.NotAcceptable;
                    }
                }

                return OperationStatus.Ok;
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all date's Events
                var dateseventStrings = datesEvents.ConvertAll(e => $"{@e}");
                var dateseventsString = String.Join(", ", dateseventStrings);

                _logger.LogError("[EventController] Error from validateEvent(): \n" +
                                 "Something went wrong when trying to validate Event " +
                                $"{@eventDTO} with list of Events {dateseventsString}, " + 
                                $"Error message: {e}");
                return OperationStatus.Error;
            }
        }


    }
}