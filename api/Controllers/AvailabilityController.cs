using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCalendar.DAL;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using System.Net;

namespace HealthCalendar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvailabilityController : ControllerBase
    {
        private readonly IAvailabilityRepo _availabilityRepo;
        private readonly ILogger<AvailabilityController> _logger;

        public AvailabilityController(IAvailabilityRepo availabilityRepo, ILogger<AvailabilityController> logger)
        {
            _availabilityRepo = availabilityRepo;
            _logger = logger;
        }

        // HTTP GET functions

        // method that retreives all Worker's availability for a week
        [HttpGet("getAllWeeksAvailability")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> getAllWeeksAvailability([FromQuery] string userId, [FromQuery] DateOnly monday)
        {
            try {
                // list of week's Availability
                var weeksAvailability = new List<Availability>();
                
                // retreives list of Worker's availability where Date = null
                var (doWAvailability, getDoWStatus) = await _availabilityRepo.getWeeksDoWAvailability(userId);
                // In case getWeeksDoWAvailability() did not succeed
                if (getDoWStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getAllWeeksAvailability(): \n" +
                                    "Could not retreive Availability with getWeeksDoWAvailability() " + 
                                    "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Week's DoW Availability");
                }

                var sunday = monday.AddDays(6);
                // retreives list of Worker's availability where Date != null and between monday and sunday
                var (dateAvailability, getDateStatus) = await _availabilityRepo
                    .getWeeksDateAvailability(userId, monday, sunday);
                // In case getWeeksDateAvailability() did not succeed
                if (getDateStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getAllWeeksAvailability(): \n" +
                                    "Could not retreive Availability with getAllWeeksDateAvailability() " + 
                                    "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Week's Date Availability");
                }

                // converts retreived Availability to AvaillibilityDTOs
                weeksAvailability.AddRange(doWAvailability);
                weeksAvailability.AddRange(dateAvailability);
                var weeksAvailabilityDTOs = weeksAvailability.Select(a => new AvailabilityDTO
                {
                    AvailabilityId = a.AvailabilityId,
                    From = a.From,
                    To = a.To,
                    DayOfWeek = a.DayOfWeek,
                    Date = a.Date,
                    UserId = userId,
                });

                return Ok(weeksAvailabilityDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from getAllWeeksAvailability(): \n" +
                                 "Something went wrong when trying to retreive week's availability from " + 
                                $"Worker with UserId = {userId} where monday is on the {monday}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that retreives all Worker's Availability for a week, excluding overlapping ones
        [HttpGet("getWeeksAvailabilityProper")]
        [Authorize(Roles="Patient,Worker")]
        public async Task<IActionResult> 
            getWeeksAvailabilityProper([FromQuery] string userId, [FromQuery] DateOnly monday)
        {
            try {
                // list of week's Availability
                var weeksAvailability = new List<Availability>();
                
                // retreives list of Worker's availability where Date = null
                var (doWAvailability, getDoWStatus) = await _availabilityRepo.getWeeksDoWAvailability(userId);
                // In case getWeeksDoWAvailability() did not succeed
                if (getDoWStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getWeeksAvailabilityProper(): \n" +
                                     "Could not retreive Availability with getWeeksDoWAvailability() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Week's DoW Availability");
                }

                var sunday = monday.AddDays(6);
                // retreives list of Worker's availability where Date != null and between monday and sunday
                var (dateAvailability, getDateStatus) = await _availabilityRepo
                    .getWeeksDateAvailability(userId, monday, sunday);
                // In case getWeeksDateAvailability() did not succeed
                if (getDateStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getWeeksAvailabilityProper(): \n" +
                                     "Could not retreive Availability with getWeeksDateAvailability() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Week's Date Availability");
                }

                // Iterates through dateAvailability and doWAvailability
                // Find overlaps safely
                var removeFromDate = new List<Availability>();
                var removeFromDoW = new List<Availability>();

                foreach (var dA in dateAvailability)
                {
                    foreach (var doWA in doWAvailability)
                    {
                        if (dA.From == doWA.From && dA.DayOfWeek == doWA.DayOfWeek)
                        {
                            removeFromDate.Add(dA);
                            removeFromDoW.Add(doWA);
                        }
                    }
                }

                // Remove after iteration
                dateAvailability = dateAvailability
                    .Except(removeFromDate)
                    .ToList();

                doWAvailability = doWAvailability
                    .Except(removeFromDoW)
                    .ToList();
                
                // converts retreived Availability to AvaillibilityDTOs
                weeksAvailability.AddRange(doWAvailability);
                weeksAvailability.AddRange(dateAvailability);
                var weeksAvailabilityDTOs = weeksAvailability.Select(a => new AvailabilityDTO
                {
                    AvailabilityId = a.AvailabilityId,
                    From = a.From,
                    To = a.To,
                    DayOfWeek = a.DayOfWeek,
                    Date = a.Date,
                    UserId = userId,
                });

                return Ok(weeksAvailabilityDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from getWeeksAvailabilityProper(): \n" +
                                 "Something went wrong when trying to retreive week's proper availability from " + 
                                $"Worker with UserId = {userId} where monday is on the {monday}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for retreiving specific AvailabilityId by DoW where Date is null
        [HttpGet("getAvailabilityIdByDoW")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            getAvailabilityIdByDoW([FromQuery] string userId, [FromQuery] DayOfWeek dayOfWeek, [FromQuery] TimeOnly from)
        {
            try
            {
                // retreives Availability
                var (availability, status) = await _availabilityRepo
                    .getAvailabilityByDoW(userId, dayOfWeek, from);
                // In case getAvailabilityByDoW() did not succeed
                if (status == OperationStatus.Error || availability == null)
                {
                    _logger.LogError("[AvailabilityController] Error from getAvailabilityIdByDoW(): \n" +
                                    "Could not retreive Availability with getAvailabilityByDoW() " + 
                                    "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }

                // retreives all AvailabilityIds from availabilityRange
                var availabilityId = availability.AvailabilityId;

                return Ok(availabilityId);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from getAvailabilityIdByDoW(): \n" +
                                 "Something went wrong when trying to retreive AvailabilityIds from " + 
                                $"range of Availability where UserId = {userId}, DayOfWeek = {dayOfWeek}, " +
                                $"Date = null and From = {from}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for retreiving AvailabilityIds from range of Availability 
        // The range of Availability is retreived using given DayOfWeek and From properties
        [HttpGet("getAvailabilityIdsByDoW")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            getAvailabilityIdsByDoW([FromQuery] string userId, [FromQuery] DayOfWeek dayOfWeek, [FromQuery] TimeOnly from)
        {
            try
            {
                // retreives list of Availability
                var (availabilityRange, status) = await _availabilityRepo
                    .getAvailabilityRangeByDoW(userId, dayOfWeek, from);
                // In case getAvailabilityRangeByDoW() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getAvailabilityIdsByDoW(): \n" +
                                    "Could not retreive Availability with getAvailabilityRangeByDoW() " + 
                                    "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }

                // retreives all AvailabilityIds from availabilityRange
                var availabilityIds = availabilityRange.Select(a => a.AvailabilityId);

                return Ok(availabilityIds);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from getAvailabilityIdsByDoW(): \n" +
                                 "Something went wrong when trying to retreive AvailabilityIds from " + 
                                $"range of Availability where UserId = {userId}, DayOfWeek = {dayOfWeek} " +
                                $"and From = {from}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // HTTP POST functions

        // method that creates new Availability and calls function to add it into table
        [HttpPost("createAvailability")]
        [Authorize(Roles="Patient,Worker")]
        public async Task<IActionResult> createAvailability([FromBody] AvailabilityDTO availabilityDTO)
        {
            try {
                // creates new Availability using availabilityDTO and worker
                var availability = new Availability
                {
                    From = availabilityDTO.From,
                    To = availabilityDTO.To,
                    DayOfWeek = availabilityDTO.DayOfWeek,
                    Date = availabilityDTO.Date,
                    UserId = availabilityDTO.UserId
                };
                var status = await _availabilityRepo.createAvailability(availability);

                // In case createAvailability() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from createAvailability(): \n" +
                                     "Could not create Availability with createAvailability() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when creating Availability");
                }
                return Ok(new { Message = "Availability has been created" });

            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from createAvailability(): \n" +
                                 "Something went wrong when trying to create new Availability " +
                                $"with AvailabilityDTO {@availabilityDTO}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for checking if Worker's Availability for given Date is continuous for Create Event function
        [HttpPost("checkAvailabilityForCreate")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> checkAvailabilityForCreate([FromBody] EventDTO eventDTO, [FromQuery] string userId)
        {
            try
            {
                var date = eventDTO.Date;
                var from = eventDTO.From;
                var to = eventDTO.To;
                
                // checks if Availability needed for Create is continuous
                var (continuousAvailabilityIds, checkStatus) = 
                    await getAndCheckAvailability(userId, date, from, to);
                // In case something went wrong in getandCheckAvailability()
                if (checkStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from checkAvailabilityForCreate(): \n" +
                                     "Could not check if Availability was continuous with " + 
                                     "getAndCheckAvailability() from AvailabilityController.");
                    return StatusCode(500, "Something went wrong when checking Availability");
                }
                // In case Availability was not continuous, status code for Not Acceptable is returned
                if (checkStatus == OperationStatus.NotAcceptable) 
                    return StatusCode(406, "Availability was not continuous");
                
                return Ok(continuousAvailabilityIds);
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[AvailabilityController] Error from checkAvailabilityForCreate(): \n" +
                                 "Something went wrong when trying to check if there" + 
                                $"was continuous Availability so Event {@eventDTO} " + 
                                $"can be created, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for checking if Worker's Availability for given Date is continuous for Update Event function
        [HttpPost("checkAvailabilityForUpdate")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> 
            checkAvailabilityForUpdate([FromBody] EventDTO updatedEventDTO, [FromQuery] DateOnly oldDate,
                                       [FromQuery] TimeOnly oldFrom, [FromQuery] TimeOnly oldTo, 
                                       [FromQuery] string userId)
        {
            try
            {
                // list of AvailabilityIds that new Schedules need to be created for
                var forCreateSchedules = new List<int>();
                // list of AvailabilityIds that existing Schedules need to be deleted for
                var forDeleteSchedules = new List<int>();
                // list of AvailabilityIds that existing Schedules need to be updated for
                var forUpdateSchedules = new List<int>();

                var updatedDate = updatedEventDTO.Date;
                var updatedFrom = updatedEventDTO.From;
                var updatedTo = updatedEventDTO.To;
                var eventId = updatedEventDTO.EventId;

                // Used to get AvailabilityIds for Schedules that need to be updated
                var updateFrom = updatedFrom;
                var updateTo = updatedTo;

                // In case update moves Event's From backwards in time, Schedules need to be created
                if (updatedDate < oldDate || updatedFrom < oldFrom)
                {
                    // New Schedules need to be created so Availability continuity needs to be checked 
                    // Must be checked from where updated Event starts to where old Event starts or updated Event ends
                    var checkTo = 
                        updatedDate < oldDate || updatedTo < oldFrom ? updatedTo : oldFrom;
                    var (continuousAvailabilityIds, checkStatus) = 
                        await getAndCheckAvailability(userId, updatedDate, updatedFrom, checkTo, eventId);
                    // In case something went wrong in getandCheckAvailability()
                    if (checkStatus == OperationStatus.Error)
                    {
                        _logger.LogError("[AvailabilityController] Error from checkAvailabilityForUpdate(): \n" +
                                         "Could not check if Availability was continuous with " + 
                                         "getAndCheckAvailability() from AvailabilityController.");
                        return StatusCode(500, "Something went wrong when checking Availability");
                    }
                    // In case Availability was not continuous, status code for Not Acceptable is returned
                    if (checkStatus == OperationStatus.NotAcceptable) 
                        return StatusCode(406, "Availability was not continuous");

                    foreach (var availabilityId in continuousAvailabilityIds)
                    {
                        forCreateSchedules.Add(availabilityId);
                    }
                    // Update of Schedules need to start where creation of these Schedules end
                    updateFrom = checkTo;
                }
                // In case update moves Event's From forwards in time, Schedules need to be deleted
                else if (oldDate < updatedDate || oldFrom < updatedFrom)
                {
                    // Existing Schedules need to be deleted so already continuous AvailabilityIds must be retreived
                    // Must be retreived from where old Event starts to where updated Event starts or old Event ends
                    var getTo = 
                        oldDate < updatedDate || oldTo < updatedFrom ? oldTo : updatedFrom;
                    var (continuousAvailabilityIds, checkStatus) = 
                        await getContinuousAvailabilityIds(userId, oldDate, oldFrom, getTo);
                    // In case something went wrong in getContinuousAvailabilityIds()
                    if (checkStatus == OperationStatus.Error)
                    {
                        _logger.LogError("[AvailabilityController] Error from checkAvailabilityForUpdate(): \n" +
                                         "Could not get Availability getContinuousAvailabilityIds()" + 
                                         "from AvailabilityController.");
                        return StatusCode(500, "Something went wrong when retreiving AvailabilityIds");
                    }

                    foreach (var availabilityId in continuousAvailabilityIds)
                    {
                        forDeleteSchedules.Add(availabilityId);
                    }
                    // Update of Schedules need to start where deletion of these Schedules end
                    updateFrom = getTo;
                }
                
                // In case update moves Event's To forwards in time, new Schedules need to be created
                if (updatedDate > oldDate || updatedTo > oldTo)
                {
                    // New Schedules need to be created so Availability continuity needs to be checked 
                    // Must be checked from where old Event ends or updated Event starts to updated Event ends
                    var checkFrom = 
                        updatedDate > oldDate || updatedFrom > oldTo ? updatedFrom : oldTo;
                    var (continuousAvailabilityIds, checkStatus) = 
                        await getAndCheckAvailability(userId, updatedDate, checkFrom, updatedTo, eventId);
                    // In case something went wrong in getandCheckAvailability()
                    if (checkStatus == OperationStatus.Error)
                    {
                        _logger.LogError("[AvailabilityController] Error from checkAvailabilityForUpdate(): \n" +
                                         "Could not check if Availability was continuous with " + 
                                         "getAndCheckAvailability() from AvailabilityController.");
                        return StatusCode(500, "Something went wrong when checking Availability");
                    }
                    // In case Availability was not continuous, status code for Not Acceptable is returned
                    if (checkStatus == OperationStatus.NotAcceptable) 
                        return StatusCode(406, "Availability was not continuous");

                    foreach (var availabilityId in continuousAvailabilityIds)
                    {
                        forCreateSchedules.Add(availabilityId);
                    }
                    // Update of Schedules need to end where creation of these Schedules start
                    updateTo = checkFrom;
                }
                // In case update moves Event's To backwards in time, Schedules need to be deleted
                else if (oldDate > updatedDate || oldTo > updatedTo)
                {
                    // Existing Schedules need to be deleted so already continuous AvailabilityIds must be retreived
                    // Must be retreived from where old Event starts or updated Event ends to where old Event ends
                    var getFrom = 
                        oldDate > updatedDate || oldFrom > updatedTo ? oldFrom : updatedTo;
                    var (continuousAvailabilityIds, checkStatus) = 
                        await getContinuousAvailabilityIds(userId, oldDate, getFrom, oldTo);
                    // In case something went wrong in getContinuousAvailabilityIds()
                    if (checkStatus == OperationStatus.Error)
                    {
                        _logger.LogError("[AvailabilityController] Error from checkAvailabilityForUpdate(): \n" +
                                         "Could not get Availability getContinuousAvailabilityIds()" + 
                                         "from AvailabilityController.");
                        return StatusCode(500, "Something went wrong when retreiving AvailabilityIds");
                    }

                    foreach (var availabilityId in continuousAvailabilityIds)
                    {
                        forDeleteSchedules.Add(availabilityId);
                    }
                    // Update of Schedules need to end where deletion of these Schedules start
                    updateTo = getFrom;
                }

                // in case Schedules need to be updated
                if (updatedDate == oldDate && updateFrom >= oldFrom && updateTo <= oldTo)
                {
                    // Retreives AvailabilityIds for schedules that must be Updated
                    var (continuousAvailabilityIds, checkStatus) = 
                        await getContinuousAvailabilityIds(userId, updatedDate, updateFrom, updateTo);
                    // In case something went wrong in getContinuousAvailabilityIds()
                    if (checkStatus == OperationStatus.Error)
                    {
                        _logger.LogError("[AvailabilityController] Error from checkAvailabilityForUpdate(): \n" +
                                         "Could not get Availability getContinuousAvailabilityIds()" + 
                                         "from AvailabilityController.");
                        return StatusCode(500, "Something went wrong when retreiving AvailabilityIds");
                    }
                    // In case Availability was not continuous, status code for Not Acceptable is returned
                    if (checkStatus == OperationStatus.NotAcceptable) 
                        return StatusCode(406, "Availability was not continuous");
                    
                    foreach (var availabilityId in continuousAvailabilityIds)
                    {
                        forUpdateSchedules.Add(availabilityId);
                    }
                }

                // Adds all lists of AvailabilityIds into container DTO
                var availabilityIdLists = new EventUpdateListsDTO
                {
                    ForCreateSchedules = forCreateSchedules.ToArray(),
                    ForDeleteSchedules = forDeleteSchedules.ToArray(),
                    ForUpdateSchedules = forUpdateSchedules.ToArray()
                };
                return Ok(availabilityIdLists);
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[AvailabilityController] Error from checkAvailabilityForCreate(): \n" +
                                 "Something went wrong when trying to check if there " + 
                                 "was continuous Availability so table can be updated with " + 
                                $"Event {@updatedEventDTO} by checking it against old Event's " + 
                                $"Date {oldDate}, From {oldFrom} and To {oldTo} properties, " + 
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // HTTP DELETE FUNCTIONS

        // method that deletes Availability from table by AvailabilityId
        [HttpDelete("deleteAvailability/{availabilityId}")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> deleteAvailability(int availabilityId)
        {
            try
            {
                // retreives Availability that should be deleted
                var (availability, getStatus) = await _availabilityRepo.getAvailabilityById(availabilityId);
                // In case getAvailabilityById() did not succeed
                if (getStatus == OperationStatus.Error || availability == null)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailability(): \n" +
                                     "Could not retreive Availability with getAvailabilityById() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }

                // deletes availability from table
                var deleteStatus = await _availabilityRepo.deleteAvailability(availability);
                // In case deleteAvailability() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailability(): \n" +
                                     "Could not delete Availability with deleteAvailability() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when deleting Availability");
                }
                
                return Ok(new { Message = "Availability has been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from deleteAvailability(): \n" +
                                 "Something went wrong when trying to delete Availability " +
                                $"with AvailabilityId = {availabilityId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that deletes range of Availability from table with list of AvailabilityId
        [HttpDelete("deleteAvailabilityByIds")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> deleteAvailabilityByIds([FromQuery] int[] availabilityIds)
        {
            try
            {
                // retreives range of Availability that should be deleted
                var (availabilityRange, getStatus) = 
                    await _availabilityRepo.getAvailabilityByIds(availabilityIds);
                // In case getAvailabilityByIds() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByIds(): \n" +
                                     "Could not retreive Availability with getAvailabilityByIds() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }

                // deletes availabilityRange from table
                var deleteStatus = await _availabilityRepo.deleteAvailabilityRange(availabilityRange);
                // In case deleteAvailabilityRange() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByIds(): \n" +
                                     "Could not delete Availability with deleteAvailabilityRange() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when deleting Availability");
                }
                
                return Ok(new { Message = "Availability has been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all AvailabilityIds
                var availabilityIdsString = String.Join(", ", availabilityIds);

                _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByIds(): \n" +
                                 "Something went wrong when trying to delete range of Availability " +
                                $"with AvailabilityIds {availabilityIdsString}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that deletes all of a Worker's Availability from table
        [HttpDelete("deleteAvailabilityByUserId")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> deleteAvailabilityByUserId([FromQuery] string userId)
        {
            try
            {
                // retreives range of Availability that should be deleted
                var (availabilityRange, getStatus) = 
                    await _availabilityRepo.getAvailabilityByUserId(userId);
                // In case getAvailabilityByIds() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByUserId(): \n" +
                                     "Could not retreive Availability with getAvailabilityByUserId() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }

                // deletes availabilityRange from table
                var deleteStatus = await _availabilityRepo.deleteAvailabilityRange(availabilityRange);
                // In case deleteAvailabilityRange() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByUserId(): \n" +
                                     "Could not delete Availability with deleteAvailabilityRange() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when deleting Availability");
                }
                
                return Ok(new { Message = "Availability has been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByUserId(): \n" +
                                 "Something went wrong when trying to delete range of Availability " +
                                $"where UserId == {userId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method that deletes range of Availability from table with given DayOfWeek and From properties
        [HttpDelete("deleteAvailabilityByDoW")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            deleteAvailabilityByDoW([FromQuery] string userId, [FromQuery] DayOfWeek dayOfWeek, [FromQuery] TimeOnly from)
        {
            try
            {
                // retreives range of Availability that should be deleted
                var (availabilityRange, getStatus) = 
                    await _availabilityRepo.getAvailabilityRangeByDoW(userId, dayOfWeek, from);
                // In case getAvailabilityRangeByDoW() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByDoW(): \n" +
                                     "Could not retreive Availability with getAvailabilityRangeByDoW() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }

                // deletes availabilityRange from table
                var deleteStatus = await _availabilityRepo.deleteAvailabilityRange(availabilityRange);
                // In case deleteAvailabilityRange() did not succeed
                if (deleteStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByDoW(): \n" +
                                     "Could not delete Availability with deleteAvailabilityRange() " + 
                                     "from AvailabilityRepo.");
                    return StatusCode(500, "Something went wrong when deleting Availability");
                }
                
                return Ok(new { Message = "Availability has been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByDoW(): \n" +
                                 "Something went wrong when trying to delete range of Availability " + 
                                $"where UserID = {userId}, DayOfWeek = {dayOfWeek} and From = {from}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // PRIVATE functions

        // method that retreives list of continuous Availability's AvailabilityIds
        private async Task<(int[], OperationStatus)> 
            getContinuousAvailabilityIds(string userId, DateOnly date, TimeOnly from, TimeOnly to)
        {
            try
            {
                // retreives relevant Availability
                var (doWAvailabilityRange, dateAvailabilityRange, getStatus) = 
                    await getTimeslotsAvailability(userId, date, from, to);
                // In case getTimeslotsAvailability() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getAndCheckAvailability(): \n" +
                                     "Could not retreive Availability with getTimeslotsAvailability() " + 
                                     "from AvailabilityController.");
                    return ([], getStatus);
                }

                
                // Converts lists of continuous Availability to list of AvailabilityIds
                var continuousAvailability = new List<Availability>();
                continuousAvailability.AddRange(doWAvailabilityRange);
                continuousAvailability.AddRange(dateAvailabilityRange);
                var continuousAvailabilityIds = 
                    continuousAvailability.Select(a => a.AvailabilityId).ToArray(); 
                    
                return (continuousAvailabilityIds, OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[AvailabilityController] Error from getContinuousAvailabilityIds(): \n" +
                                 "Something went wrong when trying to get list of continuous " + 
                                 "Availability's AvailabilityIds for Worker with UserId == " + 
                                $"{userId} on date {date} from {from} to {to}, Error message: {e}");
                return ([], OperationStatus.Error);
            }
        }
        
        // method for checking if Worker's Availability is continuous for certain timeslot
        // If it is list of continuous Availability's AvailabilityIds is returned
        private async Task<(int[], OperationStatus)> 
            getAndCheckAvailability(string userId, DateOnly date, TimeOnly from, TimeOnly to, int? excludeEventId = null) 
        {
            try
            {
                // retreives relevant Availability
                var (doWAvailabilityRange, dateAvailabilityRange, getStatus) = 
                    await getTimeslotsAvailability(userId, date, from, to);
                // In case getTimeslotsAvailability() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getAndCheckAvailability(): \n" +
                                     "Could not retreive Availability with getTimeslotsAvailability() " + 
                                     "from AvailabilityController.");
                    return ([], getStatus);
                }
                    
                // checks if doWAvailabilityRange and dateAvailabilityRange is continuous
                var (continuousAvailabilityIds, checkStatus) = 
                    checkAvailability(dateAvailabilityRange, doWAvailabilityRange, from, to);
                // In case something went wrong in checkAvailability()
                if (checkStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getAndCheckAvailability(): \n" +
                                     "Could not check if Availability was continuous with checkAvailability() " + 
                                     "from AvailabilityController.");
                    return ([], checkStatus);
                }
                // In case eventDTO was not continuous, status code for Not Acceptable is returned
                if (checkStatus == OperationStatus.NotAcceptable) return ([], checkStatus);
                    
                return (continuousAvailabilityIds, OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[AvailabilityController] Error from getAndCheckAvailability(): \n" +
                                 "Something went wrong when trying to check if there" + 
                                 "was continuous Availability for Worker with UserId == " + 
                                $"{userId} on date {date} from {from} to {to}, " + 
                                $"Error message: {e}");
                return ([], OperationStatus.Error);
            }
        }

        // method that retreives Worker's availability for given timeslot
        private async Task<(List<Availability>, List<Availability>, OperationStatus)> 
            getTimeslotsAvailability(string userId, DateOnly date, TimeOnly from, TimeOnly to)
        {
            try
            {
                // retreives relevant Availability where date is null
                var dayOfWeek = date.DayOfWeek;
                var (doWAvailabilityRange, getDoWStatus) = await _availabilityRepo
                    .getTimeslotsDoWAvailability(userId, dayOfWeek, from, to);
                // In case getTimeslotsDoWAvailability() did not succeed
                if (getDoWStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getTimeslotsAvailability(): \n" +
                                     "Could not retreive Availability with getTimeslotsDoWAvailability() " + 
                                     "from AvailabilityRepo.");
                    return ([], [], getDoWStatus);
                }

                // retreives relevant Availability where date is not null
                var (dateAvailabilityRange, getDateStatus) = await _availabilityRepo
                    .getTimeslotsDateAvailability(userId, date, from, to);
                // In case getTimeslotsDateAvailability() did not succeed
                if (getDateStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getTimeslotsAvailability(): \n" +
                                     "Could not retreive Availability with getTimeslotsDateAvailability() " + 
                                     "from AvailabilityRepo.");
                    return ([], [], getDateStatus);
                }

                return (doWAvailabilityRange, dateAvailabilityRange, OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[AvailabilityController] Error from getTimeslotsAvailability(): \n" +
                                 "Something went wrong when retreiving range of Availability where " + 
                                $"UserId = {userId}, from {from} to {to}, Error message: {e}");
                return ([], [], OperationStatus.Error);
            }
        }

        // method for checking if given lists of Availability is continuous
        private (int[], OperationStatus) checkAvailability(List<Availability> dateAvailabilityRange, 
                                                           List<Availability> doWAvailabilityRange, 
                                                           TimeOnly from, TimeOnly to)
         {
            try
            {
                // Check for overlaps between date-specific and day-of-week availability
                // If they have the same times, it's a conflict (unavailable)
                foreach (var dateAvail in dateAvailabilityRange)
                {
                    foreach (var doWAvail in doWAvailabilityRange)
                    {
                        // Check if the time slots overlap
                        if (dateAvail.From == doWAvail.From && dateAvail.To == doWAvail.To)
                        {
                            _logger.LogWarning($"[AvailabilityController] checkAvailability: Conflict - " +
                                $"date-specific and DoW availability both exist for {dateAvail.From}-{dateAvail.To}");
                            return ([], OperationStatus.NotAcceptable);
                        }
                    }
                }
                
                // Build a dictionary of time slots to availability IDs
                // Date-specific availability overrides day-of-week availability
                var availabilityMap = new Dictionary<TimeOnly, int>();
                
                // First, add day-of-week availability slots
                foreach (var doWAvailability in doWAvailabilityRange)
                {
                    availabilityMap[doWAvailability.From] = doWAvailability.AvailabilityId;
                }
                
                // Then, add date-specific availability slots (these override DoW)
                foreach (var dateAvailability in dateAvailabilityRange)
                {
                    availabilityMap[dateAvailability.From] = dateAvailability.AvailabilityId;
                }
                
                // Now check if we have continuous coverage for the requested time range
                var currentTime = from;
                var availabilityIds = new List<int>();
                
                while (currentTime < to)
                {
                    if (!availabilityMap.ContainsKey(currentTime))
                    {
                        _logger.LogWarning($"[AvailabilityController] checkAvailability: Missing slot at {currentTime}");
                        return ([], OperationStatus.NotAcceptable);
                    }
                    
                    availabilityIds.Add(availabilityMap[currentTime]);
                    currentTime = currentTime.AddMinutes(30);
                }

                return (availabilityIds.ToArray(), OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all Availability where Date is null
                var doWAvailabilityStrings = doWAvailabilityRange.ConvertAll(a => $"{@a}");
                var doWAvailabilityRangeString = String.Join(", ", doWAvailabilityStrings);
                // makes string listing all Availability where Date is not null
                var dateAvailabilityStrings = dateAvailabilityRange.ConvertAll(a => $"{@a}");
                var dateAvailabilityRangeString = String.Join(", ", dateAvailabilityStrings);

                _logger.LogError("[AvailabilityController] Error from checkAvailability(): \n" +
                                 "Something went wrong when checking if list of Availability " +
                                 "where Date is null and list of Availability where Date is not " + 
                                $"null, {doWAvailabilityRangeString} and {dateAvailabilityRangeString} " + 
                                $"is continuous from {from} to {to}, Error message: {e}");
                return ([], OperationStatus.Error);
            }
        }
    }
}
