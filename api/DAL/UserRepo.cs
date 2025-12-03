using System;
using Microsoft.AspNetCore.Identity;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthCalendar.DAL;

public class UserRepo : IUserRepo
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserRepo> _logger;

    public UserRepo(UserManager<User> userManager, ILogger<UserRepo> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    // GET FUNCTIONS:

    // method for retreiving User by their Id
    public async Task<(User?, OperationStatus)> getUserById(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return (user, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[UserRepo] Error from getUserById(): \n" +
                             "Something went wrong when retreiving User where " +
                            $"Id = {userId}, Error message: {e}");
            return (null, OperationStatus.Error);
        }
    }

    // method for retreiving Users by their Ids
    public async Task<(List<User>, OperationStatus)> getUsersByIds(string[] userIds)
    {
        try
        {
            var user = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();
            return (user, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all UserIds
            var userIdsString = String.Join(", ", userIds);
            
            _logger.LogError("[UserRepo] Error from getUsersByIds(): \n" +
                             "Something went wrong when retreiving Users with " +
                            $"Ids {userIdsString}, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method for retreiving Users by their WorkerId
    public async Task<(List<User>, OperationStatus)> getUsersByWorkerId(string workerId)
    {
        {
            try
            {
                var users = await _userManager.Users
                    .Where(u => u.WorkerId == workerId)
                    .ToListAsync();
                return (users, OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserRepo] Error from getUsersByWorkerId(): \n" +
                                "Something went wrong when retreiving Users where " +
                                $"Id = {workerId}, Error message: {e}");
                return ([], OperationStatus.Error);
            }
        }
    }

    // method for all Users with "Worker" role
    public async Task<(List<User>, OperationStatus)> getAllWorkers()
    {
        {
            try
            {
                var workers = await _userManager.Users
                    .Where(u => u.Role == Roles.Worker)
                    .ToListAsync();
                return (workers, OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserRepo] Error from getAllWorkers(): \n" +
                                "Something went wrong when retreiving Users where " +
                                $"Role = {Roles.Worker}, Error message: {e}");
                return ([], OperationStatus.Error);
            }
        }
    }

    // method for all Users with "Patient" role
    public async Task<(List<User>, OperationStatus)> getAllPatients()
    {
        {
            try
            {
                var patients = await _userManager.Users
                    .Where(u => u.Role == Roles.Patient)
                    .ToListAsync();
                return (patients, OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserRepo] Error from getAllPatients(): \n" +
                                "Something went wrong when retreiving Users where " +
                                $"Role = {Roles.Patient}, Error message: {e}");
                return ([], OperationStatus.Error);
            }
        }
    }

    // Retrieves all Workers not related to any Patient
    public async Task<(List<User>, OperationStatus)> getUnassignedPatients()
    {
        {
            try
            {
                var patients = await _userManager.Users
                    .Where(u => u.Role == Roles.Patient && u.WorkerId == null)
                    .ToListAsync();
                return (patients, OperationStatus.Ok);
            }
            catch (Exception e) // In case of unexpected exception
            {
                _logger.LogError("[UserRepo] Error from getAllPatients(): \n" +
                                "Something went wrong when retreiving Users where " +
                                $"Role = {Roles.Patient} && WorkerId = null, " + 
                                $"Error message: {e}");
                return ([], OperationStatus.Error);
            }
        }
    }

    // UPDATE FUNCTIONS:

    // method for updating User
    public async Task<OperationStatus> updateUser(User user)
    {
        try 
        {
            var result = await _userManager.UpdateAsync(user);
            // In case update did not succeed
            if (!result.Succeeded)
            {
                _logger.LogError("[UserRepo] Error from updateUser(): \n" +
                                $"User {@user} was not updated");
                return OperationStatus.Error;
            }
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[UserRepo] Error from updateUser(): \n" +
                             "Something went wrong when updating User " +
                            $"{@user}, Error message: {e}");
            return OperationStatus.Error;
        }
    }

    // DELETE FUNCTIONS:

    // method for deleting User
    public async Task<OperationStatus> deleteUser(User user)
    {
        try 
        {
            var result = await _userManager.DeleteAsync(user);
            // In case delete did not succeed
            if (!result.Succeeded)
            {
                _logger.LogError("[UserRepo] Error from deleteUser(): \n" +
                                $"User {@user} was not deleted");
                return OperationStatus.Error;
            }
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[UserRepo] Error from deleteUser(): \n" +
                             "Something went wrong when deleting User " +
                            $"{@user}, Error message: {e}");
            return OperationStatus.Error;
        }
    }
}