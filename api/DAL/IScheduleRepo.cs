using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IScheduleRepo
{
    Task<(List<Schedule>, OperationStatus)> getSchedulesByAvailabilityId(int availabilityId);
    Task<(List<Schedule>, OperationStatus)> getSchedulesByAvailabilityIds(int[] availabilityIds);
    Task<(List<Schedule>, OperationStatus)> getSchedulesByEventId(int eventId);
    Task<(List<Schedule>, OperationStatus)> getSchedulesByEventIds(int[] eventIds);
    Task<(List<Schedule>, OperationStatus)> getSchedulesAfterEventUpdate(int eventId, int[] availabilityIds);
    Task<OperationStatus> createSchedules(List<Schedule> schedules);
    Task<OperationStatus> updateSchedules(List<Schedule> schedules);
    Task<OperationStatus> deleteSchedules(List<Schedule> schedules);
}