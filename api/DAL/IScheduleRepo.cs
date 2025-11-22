using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IScheduleRepo
{
    Task<(List<Schedule>, OperationStatus)> getSchedulesByAvailabilityId(int availabilityId);
    Task<(List<Schedule>, OperationStatus)> getSchedulesByEventId(int eventId);
}