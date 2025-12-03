using Microsoft.AspNetCore.Identity;
using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IAuthRepo
{
    Task<(User?, OperationStatus)> getUserByUsername(string username);
    Task<(OperationStatus, List<IdentityError>)> registerUser(User user, string password);
    Task<OperationStatus> checkPassword(User user, string password);
    Task<OperationStatus> logout();
}