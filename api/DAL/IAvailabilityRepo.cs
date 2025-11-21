using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IAvailabilityRepo
{
    Task<OperationStatus> createAvailability(Availability availability);
    //Task<(List<Availability>?, OperationStatus)> getWeeksDoWAvailability(int workerId);
    //Task<(List<Availability>?, OperationStatus)> getWeeksDateAvailability(int workerId, DateOnly monday, DateOnly sunday);
}