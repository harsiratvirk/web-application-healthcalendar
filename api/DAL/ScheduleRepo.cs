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
}