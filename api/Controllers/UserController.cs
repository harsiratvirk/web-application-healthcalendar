using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HealthCalendar.DTOs;
using HealthCalendar.Models;
using Microsoft.EntityFrameworkCore;
using HealthCalendar.Shared;
using HealthCalendar.DAL;

namespace HealthCalendar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserController> _logger;
        private readonly HealthCalendarDbContext _db;

        public UserController(UserManager<User> userManager, ILogger<UserController> logger, HealthCalendarDbContext db)
        {
            _userManager = userManager;
            _logger = logger;
            _db = db;
        }

        // HTTP GET functions

        // Retrieves User with given Id
        [HttpGet("getUser/{userId}")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> getUser(string userId)
        {
            try
            {
                // retreives User with Id
                var user = await _userManager.FindByIdAsync(userId);
                // in case User was not retreived
                if (user == null)
                {
                    _logger.LogError("[UserController] Error from getUser(): \n" +
                                     "Could not retreive User");
                    return StatusCode(500, "Could not retreive User");
                }

                var userDTO = new UserDTO
                {
                    Id = userId,
                    UserName = user.UserName!,
                    Name = user.Name,
                    Role = user.Role,
                    WorkerId = user.WorkerId
                };
                return Ok(userDTO);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getUser(): \n" +
                                 "Something went wrong when trying to retreive User " + 
                                $"where Id = {userId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        // Retrieves Users related to Worker with given Id
        [HttpGet("getUsersByWorkerId")]
        [Authorize(Roles="Worker,Admin")]
        public async Task<IActionResult> getUsersByWorkerId([FromQuery] string workerId)
        {
            try
            {
                // retreives list of Users with "Patient" role
                // converts list into UserDTOs
                var userDTOs = await _userManager.Users
                    .Where(u => u.WorkerId == workerId)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Name = u.Name,
                        Role = u.Role,
                        WorkerId = u.WorkerId
                    }).ToListAsync();
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getUsersByWorkerId(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where WorkerId = {workerId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Retrieves all Workers
        [HttpGet("getAllWorkers")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> getAllWorkers()
        {
            try
            {
                // retreives list of Users with "Worker" role
                // converts list into UserDTOs
                var userDTOs = await _userManager.Users
                    .Where(u => u.Role == Roles.Worker)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Name = u.Name,
                        Role = u.Role
                    }).ToListAsync();
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getAllWorkers(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where Role = \"Worker\", Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Retrieves all Workers not related to any Patient
        [HttpGet("getUnassignedWorkers")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> getUnassignedWorkers()
        {
            try
            {
                // retreives list of Users with "Patient" role and a non-null WorkerId
                // converts list into WorkerIds
                var workerIds = await _userManager.Users
                    .Where(u => u.Role == Roles.Patient && u.WorkerId != null)
                    .Select(u => u.WorkerId)
                    .ToListAsync();
                
                // retreives list of Users with "Worker" role and no assigned Users with "Patient" role
                // converts list into UserDTOs
                var userDTOs = await _userManager.Users
                    .Where(u => u.Role == Roles.Worker && !workerIds.Contains(u.WorkerId))
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Name = u.Name,
                        Role = u.Role
                    }).ToListAsync();
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getUnassignedWorkers(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                 "where Role = \"Worker\" and Id is not the WorkerId " + 
                                $"of any User, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Retrieves all Patients
        [HttpGet("getAllPatients")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> getAllPatients()
        {
            try
            {
                // retreives list of Users with "Patient" role
                // converts list into UserDTOs
                var userDTOs = await _userManager.Users
                    .Where(u => u.Role == Roles.Patient)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Name = u.Name,
                        Role = u.Role
                    }).ToListAsync();
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getAllPatients(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where Role = \"Patient\", Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Retrieves all Patients not related to a Worker
        [HttpGet("getUnassignedPatients")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> getUnassignedPatients()
        {
            try
            {
                // retreives list of Users with "Patient" role and no WorkerId
                // converts list into UserDTOs
                var userDTOs = await _userManager.Users
                    .Where(u => u.Role == Roles.Patient && u.WorkerId == null)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Name = u.Name,
                        Role = u.Role
                    }).ToListAsync();
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getUnassignedPatients(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                 "where Role = \"Patient\" and WorkerId = null, " + 
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Retrieves Ids of Users related to Worker with given Id
        [HttpGet("getIdsByWorkerId")]
        [Authorize(Roles="Patient,Admin")]
        public async Task<IActionResult> getIdsByWorkerId([FromQuery] string workerId)
        {
            try
            {
                // retreives list of Users with "Patient" role
                // converts list into Ids
                var userIdss = await _userManager.Users
                    .Where(u => u.WorkerId == workerId)
                    .Select(u => u.Id)
                    .ToListAsync();
                return Ok(userIdss);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getIdsByWorkerId(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where WorkerId = {workerId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // HTTP PUT functions

        // Assigns Patient to Worker
        [HttpPut("assignPatientToWorker")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> 
            assignPatientsToWorker([FromQuery] string patientId, [FromQuery] string workerId)
        {
            try
            {
                var worker = await _userManager.FindByIdAsync(workerId);
                var patient = await _userManager.FindByIdAsync(patientId);
                // Adds Worker to Patient's Worker related parameters
                patient!.WorkerId = workerId;
                patient.Worker = worker;
                // Update table with patient
                var result = await _userManager.UpdateAsync(patient);
                // In case update did not succeed
                if (!result.Succeeded)
                {
                    _logger.LogError("[UserController] Error from assignPatientToWorker(): \n" +
                                    $"User {@patient} was not updated");
                    return StatusCode(500, "Something went wrong when assigning Patient to Worker");
                }

                return Ok(new { Message = "Patient has been assigned" });

            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from assignPatientToWorker(): \n" +
                                 "Something went wrong when trying to assign User " + 
                                $"with Id = {patientId} to User with Id = {workerId}," + 
                                $"Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Unassigns Patient from their Worker
        [HttpPut("unassignPatientFromWorker/{userId}")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> 
            unassignPatientFromWorker(string userId)
        {
            try
            {
                var patient = await _userManager.FindByIdAsync(userId);
                // Nulls out Patient's Worker related parameters
                patient!.WorkerId = null;
                patient.Worker = null;
                // Update table with patient
                var result = await _userManager.UpdateAsync(patient);
                // In case update did not succeed
                if (!result.Succeeded)
                {
                    _logger.LogError("[UserController] Error from unassignPatientFromWorker(): \n" +
                                    $"User {@patient} was not updated");
                    return StatusCode(500, "Something went wrong when assigning Patient to Worker");
                }

                return Ok(new { Message = "Patient has been unassigned" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from unassignPatientFromWorker(): \n" +
                                 "Something went wrong when trying to unassign User " + 
                                $"with Id = {userId} from their Worker, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Assigns Patients to Worker with given Username
        [HttpPut("assignPatientsToWorker")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> 
        assignPatientsToWorker([FromQuery] string[] userIds, [FromQuery] string username)
        {
            try
            {
                // retreives Worker and Patients
                var worker = await _userManager.FindByNameAsync(username);
                var patients = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();
                // Adds Worker to Patients Worker related parameters
                foreach (var patient in patients)
                {
                    patient.WorkerId = worker!.Id;
                    patient.Worker = worker;
                    // Update table with patient
                    var result = await _userManager.UpdateAsync(patient);
                    // In case update did not succeed
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("[UserController] Warning from " + 
                                           "assignPatientsToWorker(): \n" +
                                          $"User {@patient} was not updated");
                        // Error code is not returned since patients need to be updated one at a time
                        // Returning something would stop the entire operation
                    }
                }

                return Ok(new { Message = "Patients have been assigned" });

            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all UserIds
                var userIdsString = String.Join(", ", userIds);

                _logger.LogError("[UserController] Error from assignPatientsToWorker(): \n" +
                                 "Something went wrong when trying to assign Users " + 
                                $"with Ids {userIdsString} to User with UserName = " + 
                                $"{username}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }


        // HTTP DELETE functions

        [HttpDelete("deleteUser/{userId}")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> deleteUser(string userId)
        {
            try
            {
                // retreives User that should be deleted
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    _logger.LogError("[UserController] Error from deleteUser(): \n" +
                                     "User not found");
                    return NotFound("User not found");
                }

                // If user is a Worker, unassign all their patients and delete availability records
                if (user.Role == Roles.Worker)
                {
                    // Delete all availability records for this worker
                    var availabilities = await _db.Availability
                        .Where(a => a.UserId == userId)
                        .ToListAsync();
                    
                    if (availabilities.Any())
                    {
                        _db.Availability.RemoveRange(availabilities);
                        await _db.SaveChangesAsync();
                        _logger.LogInformation($"[UserController] Deleted {availabilities.Count} availability records for worker {userId}");
                    }
                    
                    // Unassign all patients from this worker
                    var assignedPatients = await _userManager.Users
                        .Where(u => u.WorkerId == userId)
                        .ToListAsync();
                    
                    foreach (var patient in assignedPatients)
                    {
                        patient.WorkerId = null;
                        patient.Worker = null;
                        await _userManager.UpdateAsync(patient);
                    }
                    
                    _logger.LogInformation($"[UserController] Unassigned {assignedPatients.Count} patients from worker {userId}");
                }
                
                // deletes user from table
                var result = await _userManager.DeleteAsync(user);
                // In case deletion did not succeed
                if (!result.Succeeded)
                {
                    _logger.LogError("[UserController] Error from deleteUser(): \n" +
                                    $"User {@user} was not deleted");
                    return StatusCode(500, "Something went wrong when deleting user");
                }
                
                return Ok(new { Message = "User has been deleted" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from deleteUser(): \n" +
                                 "Something went wrong when trying to delete User " +
                                $"with Id = {userId}, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}