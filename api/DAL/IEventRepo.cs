using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IEventRepo
{
    Task<(Event?, OperationStatus)> getEventById(int eventId);
    Task<(List<Event>, OperationStatus)> getEventsByIds(int[] eventIds);
    Task<(List<Event>, OperationStatus)> getEventsByUserId(string userId);
    Task<(List<Event>, OperationStatus)> getDatesEvents(string[] userIds, DateOnly date);
    Task<(List<Event>, OperationStatus)> getWeeksEventsByUserIds(string[] userIds, DateOnly monday, DateOnly sunday);
    Task<(List<Event>, OperationStatus)> getWeeksEventsByUserId(string userId, DateOnly monady, DateOnly sunday);
    Task<OperationStatus> createEvent(Event eventt);
    Task<OperationStatus> updateEvent(Event eventt);
    Task<OperationStatus> deleteEvent(Event eventt);
    Task<OperationStatus> deleteEvents(List<Event> events);
}