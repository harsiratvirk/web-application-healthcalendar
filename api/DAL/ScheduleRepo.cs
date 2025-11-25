using System;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthCalendar.DAL;

public class ScheduleRepo : IScheduleRepo
{
    private readonly HealthCalendarDbContext _db;
    private readonly ILogger<ScheduleRepo> _logger;
    public ScheduleRepo(HealthCalendarDbContext db, ILogger<ScheduleRepo> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET FUNCTIONS:

    // method that returns all Schedules with given AvailabilityId
    public async Task<(List<Schedule>, OperationStatus)> getSchedulesByAvailabilityId(int availabilityId)
    {
        try
        {
            var schedules = await _db.Schedule
                .Where(s => s.AvailabilityId == availabilityId)
                .ToListAsync();
            return (schedules, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[ScheduleRepo] Error from getSchedulesByAvailabilityId(): \n" +
                             "Something went wrong when retreiving Schedules where " +
                            $"AvailabilityId = {availabilityId}, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method that returns all Schedules with AvailabilityId in list of AvailabilityIds
    public async Task<(List<Schedule>, OperationStatus)> getSchedulesByAvailabilityIds(int[] availabilityIds)
    {
        try
        {
            var schedules = await _db.Schedule
                .Where(s => availabilityIds.Contains(s.AvailabilityId))
                .ToListAsync();
            return (schedules, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all AvailabilityIds
            var availabilityIdsString = String.Join(", ", availabilityIds);

            _logger.LogError("[ScheduleRepo] Error from getSchedulesByAvailabilityIds(): \n" +
                             "Something went wrong when retreiving Schedules with list of" +
                            $"AvailabilityIds {availabilityIdsString}, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method that returns all Schedules with given eventId
    public async Task<(List<Schedule>, OperationStatus)> getSchedulesByEventId(int eventId)
    {
        try
        {
            var schedules = await _db.Schedule
                .Where(s => s.EventId == eventId)
                .ToListAsync();
            return (schedules, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[ScheduleRepo] Error from getSchedulesByEventId(): \n" +
                             "Something went wrong when retreiving Schedules where " +
                            $"EventId = {eventId}, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method that returns all Schedules with EventId in list of EventIds
    public async Task<(List<Schedule>, OperationStatus)> getSchedulesByEventIds(int[] eventIds)
    {
        try
        {
            var schedules = await _db.Schedule
                .Where(s => eventIds.Contains(s.EventId))
                .ToListAsync();
            return (schedules, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all EventIds
            var eventIdsString = String.Join(", ", eventIds);

            _logger.LogError("[ScheduleRepo] Error from getSchedulesByEventIds(): \n" +
                             "Something went wrong when retreiving Scheduless with list of " +
                            $"EventIds {eventIdsString}, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method for retreiving necessary Schedules for deletion and updating of Schedules after Event deletion
    public async Task<(List<Schedule>, OperationStatus)> getSchedulesAfterEventUpdate(int eventId, int[] availabilityIds)
    {
        try
        {
            var schedules = await _db.Schedule
                .Where(s => s.EventId == eventId && availabilityIds.Contains(s.AvailabilityId))
                .ToListAsync();
            return (schedules, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all AvailabilityIds
            var availabilityIdsString = String.Join(", ", availabilityIds);

            _logger.LogError("[ScheduleRepo] Error from getSchedulesAfterEventUpdate(): \n" +
                             "Something went wrong when retreiving Scheduless with " +
                            $"EventId {eventId} and AvailabilityIds {availabilityIdsString}, " + 
                            $"Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    
    // CREATE FUNCTIONS:

    // Adds range of Schedules to table
    public async Task<OperationStatus> createSchedules(List<Schedule> schedules)
    {
        try
        {
            _db.Schedule.AddRange(schedules);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all schedules
            var scheduleStrings = schedules.ConvertAll(s => $"{@s}");
            var schedulesString = String.Join(", ", scheduleStrings);
            
            _logger.LogError("[ScheduleRepo] Error from createSchedules(): \n" +
                             "Something went wrong when creating Schedules " +
                            $"{schedulesString}, Error message: {e}");
            return OperationStatus.Error;
        }
    }

    // UPDATE FUNCTIONS:

    // Updates table with given range of Schedules
    public async Task<OperationStatus> updateSchedules(List<Schedule> schedules)
    {
        try 
        {
            _db.UpdateRange(schedules);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all schedules
            var scheduleStrings = schedules.ConvertAll(s => $"{@s}");
            var schedulesString = String.Join(", ", scheduleStrings);
            
            _logger.LogError("[ScheduleRepo] Error from updateSchedules(): \n" +
                             "Something went wrong when updating range of Schedules " +
                            $"{schedulesString}, Error message: {e}");
            return OperationStatus.Error;
        }
    }


    // DELETE FUNCTIONS:

    // method that deletes list of Schedules from table
    public async Task<OperationStatus> deleteSchedules(List<Schedule> schedules)
    {
        try 
        {
            _db.RemoveRange(schedules);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all schedules
            var scheduleStrings = schedules.ConvertAll(s => $"{@s}");
            var schedulesString = String.Join(", ", scheduleStrings);
            
            _logger.LogError("[ScheduleRepo] Error from deleteSchedules(): \n" +
                             "Something went wrong when deleting range of Schedules " +
                            $"{schedulesString}, Error message: {e}");
            return OperationStatus.Error;
        }
    }

}