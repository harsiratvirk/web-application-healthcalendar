using System;
using Microsoft.AspNetCore.Identity;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthCalendar.DAL;

public class AuthRepo : IAuthRepo
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthRepo> _logger;

    public AuthRepo(UserManager<User> userManager, SignInManager<User> signInManager, 
                        ILogger<AuthRepo> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // GET FUNCTIONS:

    // method for retreiving User by UserName
    public async Task<(User?, OperationStatus)> getUserByUsername(string username)
    {
        try 
        {
            var user = await _userManager.FindByNameAsync(username);
            return (user, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {   
            _logger.LogError("[AuthRepo] Error from getUserByUsername(): \n" +
                             "Something went wrong when retreiving User, " +
                            $"Error message: {e}");
            return (null, OperationStatus.Error);
        }
    }


    // POST FUNCTIONS:

    // method for registering User
    public async Task<(OperationStatus, List<IdentityError>)> registerUser(User user, string password)
    {
        try 
        {
            // Attempts to create new user, automatically hashes passoword
            var result = await _userManager.CreateAsync(user, password);
            // For when registration doesn't succeed
            if (!result.Succeeded)
            {
                _logger.LogWarning("[AuthRepo] Warning from registerUser(): \n" +
                                  $"Registration failed for Patient: {user.Name}");
                var errors = result.Errors.ToList();
                return (OperationStatus.Error, errors);
            }
            return (OperationStatus.Ok, []);
        }
        catch (Exception e) // In case of unexpected exception
        {
            // Custom error
            var errors = new List<IdentityError> 
            {
                new IdentityError {
                    Code = "500",
                    Description = "Internal Server Error"
                }
            };
            
            _logger.LogError("[AuthRepo] Error from registerUser(): \n" +
                             "Something went wrong when registering User, " +
                            $"Error message: {e}");
            return (OperationStatus.Error, errors);
        }
    }

    // method for checking if password is correct
    public async Task<OperationStatus> checkPassword(User user, string password)
    {
        try 
        {
            // checks user's password
            if (await _userManager.CheckPasswordAsync(user, password))
            {
                return OperationStatus.Ok;
            }
            else 
            {
                return OperationStatus.Unauthorized;
            }
        }
        catch (Exception e) // In case of unexpected exception
        {
            
            _logger.LogError("[AuthRepo] Error from registerUser(): \n" +
                             "Something went wrong when checking password, " +
                            $"Error message: {e}");
            return OperationStatus.Error;
        }
    }

    // method for clearing eventual cookies in the server
    public async Task<OperationStatus> logout()
    {
        try 
        {
            await _signInManager.SignOutAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AuthRepo] Error from logout(): \n" +
                             "SOmething went wrong when logging out, " +
                            $"Error message: {e}");
            return OperationStatus.Error;
        }
    }
}