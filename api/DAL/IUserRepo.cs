using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IUserRepo
{
    Task<(User?, OperationStatus)> getUserById(string userId);
    Task<(List<User>, OperationStatus)> getUsersByIds(string[] userIds);
    Task<(List<User>, OperationStatus)> getUsersByWorkerId(string workerId);
    Task<(List<User>, OperationStatus)> getAllWorkers();
    Task<(List<User>, OperationStatus)> getAllPatients();
    Task<(List<User>, OperationStatus)> getUnassignedPatients();
    Task<OperationStatus> updateUser(User user);
    Task<OperationStatus> deleteUser(User user);
}