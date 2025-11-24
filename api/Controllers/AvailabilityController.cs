using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        
        // userManager used to retreive Users related to Availability upon creation
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AvailabilityController> _logger;

        public AvailabilityController(IAvailabilityRepo availabilityRepo, UserManager<User> userManager, 
                                      ILogger<AvailabilityController> logger)
        {
            _availabilityRepo = availabilityRepo;
            _userManager = userManager;
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
                dateAvailability.ForEach(dA => {
                    var from = dA.From;
                    var dayOfWeek = dA.DayOfWeek;
                    doWAvailability.ForEach(doWA =>
                    {
                        // Removes Availability that overlap with eachother
                        if (from == doWA.From && dayOfWeek == doWA.DayOfWeek)
                        {
                            dateAvailability.Remove(dA);
                            doWAvailability.Remove(doWA);
                            return;
                        }
                    });
                });
                
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

        // method for retreiving AvailabilityIds from range of Availability 
        // The range of Availability is retreived using given DayOfWeek and From properties
        [HttpGet("getAvailabilityIdsByDoW")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            getAvailabilityIdsByDoW([FromQuery] DayOfWeek dayOfWeek,[FromQuery] TimeOnly from)
        {
            try
            {
                // retreives list of Availability
                var (availabilityRange, status) = await _availabilityRepo
                    .getAvailabilityByDoW(dayOfWeek, from);
                // In case getAvailabilityByDoW() did not succeed
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from getAvailabilityByDoW(): \n" +
                                    "Could not retreive Availability with getAvailabilityByDoW() " + 
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
                                $"range of Availability where DayOfWeek = {dayOfWeek} and From = {from}, " +
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // method for checking if Worker's Availability for given Date is continuous for Create Event function
        [HttpGet("checkAvailabilityForCreate")]
        [Authorize(Roles="Patient")]
        public async Task<IActionResult> checkAvailabilityForCreate([FromBody] Event eventDTO)
        {
            try
            {
                var userId = eventDTO.UserId;
                var date = eventDTO.Date;
                var from = eventDTO.From;
                var to = eventDTO.To;

                // retreives relevant Availability
                var (doWAvailabilityRange, dateAvailabilityRange, getStatus) = 
                    await getTimeslotsAvailability(userId, date, from, to);
                // In case getTimeslotsAvailability() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from checkAvailabilityForCreate(): \n" +
                                     "Could not retreive Availability with getTimeslotsAvailability() " + 
                                     "from AvailabilityController.");
                    return StatusCode(500, "Something went wrong when retreiving Availability");
                }
                
                // checks if doWAvailabilityRange and dateAvailabilityRange is continuous
                var (continuousAvailabilityIds, validationStatus) = 
                    checkAvailability(dateAvailabilityRange, doWAvailabilityRange, from, to);
                // In case something went wrong in checkAvailability()
                if (validationStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from checkAvailabilityForCreate(): \n" +
                                     "Could not check if Availability was continuous with checkAvailability() " + 
                                     "from AvailabilityController.");
                    return StatusCode(500, "Something went wrong when validating Event");
                }
                // In case eventDTO was not continuous, status code for Not Acceptable is returned
                if (validationStatus == OperationStatus.NotAcceptable) 
                    return StatusCode(406, "Availability was not continuous");
                
                return Ok(continuousAvailabilityIds);
            }
            catch (Exception e) // In case of unexpected exception
            {   
                _logger.LogError("[AvailabilityController] Error from checkAvailabilityForCreate(): \n" +
                                 "Something went wrong when trying to check if there" + 
                                $"was continuous Availability so Event {eventDTO} " + 
                                $"can be created, Error message: {e}");
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
                // retreives Worker and adds it into availabilityDTO
                var userId = availabilityDTO.UserId;
                var worker = await _userManager.FindByIdAsync(userId);
                
                // creates new Availability using availabilityDTO and worker
                var availability = new Availability
                {
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

        // method that deletes range of Availability from table with given DayOfWeek and From properties
        [HttpDelete("deleteAvailabilityByDoW")]
        [Authorize(Roles="Worker")]
        public async Task<IActionResult> 
            deleteAvailabilityByDoW([FromQuery] DayOfWeek dayOfWeek, [FromQuery] TimeOnly from)
        {
            try
            {
                // retreives range of Availability that should be deleted
                var (availabilityRange, getStatus) = 
                    await _availabilityRepo.getAvailabilityByDoW(dayOfWeek, from);
                // In case getAvailabilityByDoW() did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[AvailabilityController] Error from deleteAvailabilityByDoW(): \n" +
                                     "Could not retreive Availability with getAvailabilityByDoW() " + 
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
                                $"where DayOfWeek = {dayOfWeek} and From = {from}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // PRIVATE functions

        // method that retreives Worker's availability
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
                    return ([], [], OperationStatus.Error);
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
                    return ([], [], OperationStatus.Error);
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
                var continuousAvailability = new List<Availability>();
                // calculates factor of difference between to and from, divided by 30 min
                var timeDifference = to.ToTimeSpan() - from.ToTimeSpan();
                var wantedSize = timeDifference.Divide(TimeSpan.FromMinutes(30));
                // size of both Availability lists combined must be equal to wantedSize 
                // If not, Availability cannot be continuous
                if ((dateAvailabilityRange.Count() + doWAvailabilityRange.Count()) != wantedSize)
                {
                    _logger.LogInformation("[EventController] Information from " + 
                                           "checkAvailability(): \n" +
                                          $"Availability from given lists was not continuous.");
                    return ([], OperationStatus.NotAcceptable);
                }

                // Iterates through dateAvailabilityRange and doWAvailabilityRange
                // looks for availability that overlap
                foreach (var dateAvailability in dateAvailabilityRange)
                {
                    var dateFrom = dateAvailability.From;
                    foreach (var doWAvailability in doWAvailabilityRange)
                    {
                        // checks if dateAvailability and doWAvailability overlap
                        if (dateFrom == doWAvailability.From)
                        {
                            _logger.LogInformation("[EventController] Information from " + 
                                                   "checkAvailability(): \n" +
                                                  $"Availability from given lists was not continuous.");
                            return ([], OperationStatus.NotAcceptable);
                        }
                    }
                }

                // Converts lists of Availability to list of AvailabilityIds
                continuousAvailability.AddRange(doWAvailabilityRange);
                continuousAvailability.AddRange(dateAvailabilityRange);
                var continuousAvailabilityIds = 
                    continuousAvailability.Select(a => a.AvailabilityId).ToArray();

                return (continuousAvailabilityIds, OperationStatus.Ok);
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
