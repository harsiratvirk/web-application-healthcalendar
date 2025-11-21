using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IAvailabilityRepo
{
    Task<OperationStatus> createAvailability(Availability availability);
    Task<(List<Availability>, OperationStatus)> getWeeksDoWAvailability(string userId);
    Task<(List<Availability>?, OperationStatus)> getWeeksDateAvailability(string userId, DateOnly monday, DateOnly sunday);
}