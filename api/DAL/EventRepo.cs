using System;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthCalendar.DAL;

public class EventRepo : IEventRepo
{
    private readonly HealthCalendarDbContext _db;
    private readonly ILogger<EventRepo> _logger;
    public EventRepo(HealthCalendarDbContext db, ILogger<EventRepo> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET FUNCTIONS:

    // method for retreiving Event by eventId
    public async Task<(Event?, OperationStatus)> getEventById(int eventId)
    {
        try
        {
            var eventt = await _db.Events.FindAsync(eventId);
            return (eventt, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[EventRepo] Error from getEventById(): \n" +
                             "Something went wrong when retreiving Event where " +
                            $"EventId = {eventId}, Error message: {e}");
            return (null, OperationStatus.Error);
        }
    }

    // method for retreiving range of Events with list of EventIds
    public async Task<(List<Event>, OperationStatus)> getEventsByIds(int[] eventIds)
    {
        try
        {
            var events = await _db.Events
                .Where(a => eventIds.Contains(a.EventId))
                .ToListAsync();
            return (events, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all EventIds
            var eventIdsString = String.Join(", ", eventIds);

            _logger.LogError("[EventRepo] Error from getEventsByIds(): \n" +
                             "Something went wrong when retreiving range of Events " +
                            $"with list of EventIds {eventIdsString}, " + 
                            $"Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method for retreiving Patient's Events for specific date
    public async Task<(List<Event>, OperationStatus)> getEventsByDate(string userId, DateOnly date)
    {
        try
        {
            var events = await _db.Events
                .Where(e => e.UserId == userId && e.Date == date)
                .ToListAsync();
            return (events, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[EventRepo] Error from getEventsByDate(): \n" +
                             "Something went wrong when retreiving Events where " +
                            $"UserId = {userId}, and Date == {date}, " +
                            $"Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method for retreiving a Patient's Events for the week
    public async Task<(List<Event>, OperationStatus)> 
        getWeeksEventsForPatient(string userId, DateOnly monday, DateOnly sunday)
    {
        try
        {
            // retreives events between given dates for monday and sunday
            var events = await _db.Events
                .Where(e => e.UserId == userId && e.Date >= monday && e.Date <= sunday)
                .ToListAsync();
            return (events, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[EventRepo] Error from getWeeksEventsForPatient(): \n" +
                             "Something went wrong when retreiving Events where " +
                            $"UserId = {userId}, and Date is between {monday} and {sunday}, " +
                            $"Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }


    // CREATE FUNCTIONS:

    // method for adding Event into table
    public async Task<OperationStatus> createEvent(Event eventt)
    {
        try
        {
            _db.Events.Add(eventt);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[EventRepo] Error from createEvent(): \n" +
                             "Something went wrong when creating Event " +
                            $"{@eventt}, Error message: {e}");
            return OperationStatus.Error;
        }
    }


    // UPDATE FUNCTIONS:

    // method for updating Event
    public async Task<OperationStatus> updateEvent(Event eventt)
    {
        try 
        {
            _db.Update(eventt);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[EventRepo] Error from updateEvent(): \n" +
                             "Something went wrong when updating Event " +
                            $"{@eventt}, Error message: {e}");
            return OperationStatus.Error;
        }
    }


    // DELETE FUNCTIONS:

    // method for deleting Event from table
    public async Task<OperationStatus> deleteEvent(Event eventt)
    {
        try 
        {
            _db.Remove(eventt);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[EventRepo] Error from deleteEvent(): \n" +
                             "Something went wrong when deleting Event " +
                            $"{@eventt}, Error message: {e}");
            return OperationStatus.Error;
        }
    }

    // method for deleting range of Events from table
    public async Task<OperationStatus> deleteEvents(List<Event> events)
    {
        try 
        {
            _db.RemoveRange(events);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all Events
            var eventStrings = events.ConvertAll(e => $"{@e}");
            var eventsString = String.Join(", ", eventStrings);
            
            _logger.LogError("[EventRepo] Error from deleteEvents(): \n" +
                             "Something went wrong when deleting range of Events " +
                            $"{eventsString}, Error message: {e}");
            return OperationStatus.Error;
        }
    }
}
