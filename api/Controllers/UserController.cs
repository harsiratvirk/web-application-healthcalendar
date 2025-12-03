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
        private readonly IUserRepo _userRepo;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserRepo userRepo, ILogger<UserController> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
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
                var (user, status) = await _userRepo.getUserById(userId);
                // in case User was not retreived
                if (status == OperationStatus.Error || user == null)
                {
                    _logger.LogError("[UserController] Error from getUser(): \n" +
                                     "Could not retreive User with getUserById() " + 
                                     "from UserRepo");
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
                // retreives list of Users by their WorkerId
                var (users, status) = await _userRepo.getUsersByWorkerId(workerId);
                // In case error occured
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[UserController] Error from getUsersByWorkerId(): \n" +
                                     "Could not retreive User with getUsersByWorkerId() " + 
                                     "from UserRepo");
                    return StatusCode(500, "Could not retreive Users");
                }
                
                // converts list into UserDTOs
                var userDTOs = users.Select(u => new UserDTO
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        Name = u.Name,
                        Role = u.Role,
                        WorkerId = u.WorkerId
                    });
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
                var (workers, status) = await _userRepo.getAllWorkers();
                // In case error occured
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[UserController] Error from getAllWorkers(): \n" +
                                     "Could not retreive User with getAllWorkers() " + 
                                     "from UserRepo");
                    return StatusCode(500, "Could not retreive Users");
                }
                
                // converts list into UserDTOs
                var userDTOs = workers.Select(u => new UserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Name = u.Name,
                    Role = u.Role,
                    WorkerId = u.WorkerId
                });
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getAllWorkers(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where Role = {Roles.Worker}, Error message: {e}");
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
                var (patients, status) = await _userRepo.getAllPatients();
                // In case error occured
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[UserController] Error from getAllPatients(): \n" +
                                     "Could not retreive User with getAllPatients() " + 
                                     "from UserRepo");
                    return StatusCode(500, "Could not retreive Users");
                }
                
                // converts list into UserDTOs
                var userDTOs = patients.Select(u => new UserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Name = u.Name,
                    Role = u.Role,
                    WorkerId = u.WorkerId
                });
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getAllPatients(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where Role = {Roles.Patient}, Error message: {e}");
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
                var (patients, status) = await _userRepo.getUnassignedPatients();
                // In case error occured
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[UserController] Error from getUnassignedPatients(): \n" +
                                     "Could not retreive User with getUnassignedPatients() " + 
                                     "from UserRepo");
                    return StatusCode(500, "Could not retreive Users");
                }
                
                // converts list into UserDTOs
                var userDTOs = patients.Select(u => new UserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Name = u.Name,
                    Role = u.Role,
                    WorkerId = u.WorkerId
                });
                return Ok(userDTOs);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserController] Error from getUnassignedPatients(): \n" +
                                 "Something went wrong when trying to retreive Users " + 
                                $"where Role = {Roles.Patient} and WorkerId = null, " + 
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
                // retreives list of Users by their WorkerId
                var (users, status) = await _userRepo.getUsersByWorkerId(workerId);
                // In case error occured
                if (status == OperationStatus.Error)
                {
                    _logger.LogError("[UserController] Error from getIdsByWorkerId(): \n" +
                                     "Could not retreive User with getUsersByWorkerId() " + 
                                     "from UserRepo");
                    return StatusCode(500, "Could not retreive Users");
                }

                var userIds = users.Select(u => u.Id);
                return Ok(userIds);
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

        // Unassigns Patient from their Worker
        [HttpPut("unassignPatientFromWorker/{userId}")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> 
            unassignPatientFromWorker(string userId)
        {
            try
            {
                var (patient, getStatus) = await _userRepo.getUserById(userId);
                // in case User was not retreived
                if (getStatus == OperationStatus.Error || patient == null)
                {
                    _logger.LogError("[UserController] Error from unassignPatientFromWorker(): " + 
                                     "Patient not found with getUserById() from UserRepo");
                    return NotFound("Patient not found");
                }
                
                // Nulls out Patient's Worker related parameters
                patient.WorkerId = null;
                patient.Worker = null;
                // Update table with patient
                var updateStatus = await _userRepo.updateUser(patient);
                // In case update did not succeed
                if (updateStatus == OperationStatus.Error)
                {
                    _logger.LogError("[UserController] Error from unassignPatientFromWorker(): \n" +
                                    $"User was not updated with updateUser() from UserRepo");
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

        // Unassigns Patients from their Worker
        [HttpPut("unassignPatientsFromWorker")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> unassignPatientsFromWorker([FromQuery] string[] userIds)
        {
            try
            {
                
                var (patients, getStatus) = await _userRepo.getUsersByIds(userIds);
                // in case Users were not retreived
                if (getStatus == OperationStatus.Error || !patients.Any())
                {
                    _logger.LogError("[UserController] Error from unassignPatientsFromWorker(): " + 
                                     "Patient not found with getUsersByIds() from UserRepo");
                    return NotFound("Patients not found");
                }
                
                // Nulls out Patients Worker related parameters one at a time
                foreach (var patient in patients)
                {
                    patient.WorkerId = null;
                    patient.Worker = null;
                    // Update table with patient
                    var updateStatus = await _userRepo.updateUser(patient);
                    // In case update did not succeed
                    if (updateStatus == OperationStatus.Error)
                    {
                        _logger.LogWarning("[UserController] Warning from " + 
                                           "unassignPatientsFromWorker(): \n" +
                                           "updateUser() from UserRepo did not" +
                                          $"update patient {@patient}");
                        // Error code is not returned since patients need to be updated one at a time
                        // Returning something would stop the entire operation
                    }
                }

                return Ok(new { Message = "Patients have been unassigned" });
            }
            catch (Exception e) // In case of unexpected exception
            {
                // makes string listing all UserIds
                var userIdsString = String.Join(", ", userIds);

                _logger.LogError("[UserController] Error from unassignPatientsFromWorker(): \n" +
                                 "Something went wrong when trying to unassign User " + 
                                $"with Ids {userIdsString} from their Worker, Error message: {e}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Assigns Patients to Worker with given Username
        [HttpPut("assignPatientsToWorker")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> 
        assignPatientsToWorker([FromQuery] string[] userIds, [FromQuery] string workerId)
        {
            try
            {
                // retreives Worker
                var (worker, getWorkerStatus) = await _userRepo.getUserById(workerId);
                // in case Worker was not retreived
                if (getWorkerStatus == OperationStatus.Error || worker == null)
                {
                    _logger.LogError("[UserController] Error from assignPatientsToWorker(): " + 
                                     "Worker not found with getUserById() from UserRepo");
                    return NotFound("Worker not found");
                }
                // retreives Patients
                var (patients, getPatientStatus) = await _userRepo.getUsersByIds(userIds);
                // in case Patient was not retreived
                if (getWorkerStatus == OperationStatus.Error || !patients.Any())
                {
                    _logger.LogError("[UserController] Error from assignPatientsToWorker(): " + 
                                     "Patients not found with getUsersByIds() from UserRepo");
                    return NotFound("Patients not found");
                }
                
                // Adds Worker to Patients Worker-related parameters
                foreach (var patient in patients)
                {
                    patient.WorkerId = worker!.Id;
                    patient.Worker = worker;
                    // Update table with patient
                    var updateStatus = await _userRepo.updateUser(patient);
                    // In case update did not succeed
                    if (getWorkerStatus == OperationStatus.Error)
                    {
                        _logger.LogWarning("[UserController] Warning from " + 
                                           "assignPatientsToWorker(): \n" +
                                           "updateUser() from UserRepo did not" +
                                          $"update patient {@patient}");
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
                                $"with Ids {userIdsString} to User with Id = " + 
                                $"{workerId}, Error message: {e}");
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
                var (user, getStatus) = await _userRepo.getUserById(userId);
                
                if (getStatus == OperationStatus.Error || user == null)
                {
                    _logger.LogError("[UserController] Error from deleteUser(): \n" +
                                     "User not found with getUserById() from UserRepo");
                    return NotFound("User not found");
                }
                
                // deletes user from table
                var deleteStatus = await _userRepo.deleteUser(user);
                // In case deletion did not succeed
                if (getStatus == OperationStatus.Error)
                {
                    _logger.LogError("[UserController] Error from deleteUser(): \n" +
                                    $"User {@user} was not deleted with deleteUser() " + 
                                    "from UserRepo");
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