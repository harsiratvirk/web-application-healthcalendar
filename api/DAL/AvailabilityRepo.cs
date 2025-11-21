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
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepo] Error from createAvailability(): \n" +
                             "Something went wrong when creating Availability " +
                            $"{@availability}, Error message: {e}");
            return OperationStatus.Error;
        }
    }
    //public async Task<(List<Availability>?, OperationStatus)> getWeeksDoWAvailability(int workerId);
    //public async Task<(List<Availability>?, OperationStatus)> getWeeksDateAvailability(int workerId, DateOnly monday, DateOnly sunday);
}
