using System.Data.Common;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthCalendar.DAL;

public class AvailabilityRepo : IAvailabilityRepo
{
    private readonly HealthCalendarDbContext _db;
    private readonly ILogger<AvailabilityRepo> _logger;
    public AvailabilityRepo(HealthCalendarDbContext db, ILogger<AvailabilityRepo> logger)
    {
        _db = db;
        _logger = logger;
    }

    // method for adding Availability into database
    public async Task<OperationStatus> createAvailability(Availability availability)
    {
        try
        {
            _db.Availability.Add(availability);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from createAvailability(): \n" +
                             "Something went wrong when creating Availability " +
                            $"{@availability}, Error message: {e}");
            return OperationStatus.Error;
        }
    }

    // method for retreiving a Worker's Availability where Date is null
    public async Task<(List<Availability>, OperationStatus)> getWeeksDoWAvailability(string userId)
    {
        try
        {
            var availability = await _db.Availability
                .Where(a => a.UserId == userId && a.Date == null)
                .ToListAsync();
            return (availability, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from getWeeksDoWAvailability(): \n" +
                             "Something went wrong when retreiving Availability where " +
                            $"workerId = {userId} and Date = null, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method for retreiving a Worker's Availability for the week where Date is not null
    public async Task<(List<Availability>?, OperationStatus)> 
        getWeeksDateAvailability(string userId, DateOnly monday, DateOnly sunday)
    {
        try
        {
            // retreives list of availability between given dates for monday and sunday
            var availability = await _db.Availability
                .Where(a => a.UserId == userId && a.Date >= monday && a.Date <= sunday)
                .ToListAsync();
            return (availability, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from getWeeksDateAvailability(): \n" +
                             "Something went wrong when retreiving Availability where " +
                            $"workerId = {userId} and Date = null, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }
}
