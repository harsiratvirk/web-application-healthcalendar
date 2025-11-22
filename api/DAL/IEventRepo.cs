using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IEventRepo
{
    Task<(Event?, OperationStatus)> getEventById(int eventId);
    Task<(List<Event>, OperationStatus)> getEventsByIds(int[] eventIds);
    Task<OperationStatus> deleteEvent(Event eventt);
    Task<OperationStatus> deleteEvents(List<Event> events);
}