using System;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthCalendar.DAL;

public class ScheduleRepo : IScheduleRepo
{
    private readonly HealthCalendarDbContext _db;
    private readonly ILogger<AvailabilityRepo> _logger;
    public ScheduleRepo(HealthCalendarDbContext db, ILogger<AvailabilityRepo> logger)
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
            // converts schedules to string listing all schedules
            var scheduleStrings = schedules.ConvertAll(s => $"{@s}");
            var schedulesString = String.Join(", ", scheduleStrings);
            
            _logger.LogError("[ScheduleRepo] Error from deleteSchedules(): \n" +
                             "Something went wrong when deleting range of Schedules " +
                            $"{schedulesString}, Error message: {e}");
            return OperationStatus.Error;
        }
    }

}