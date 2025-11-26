using System;
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

    // GET FUNCTIONS:

    // method for retreiving Availability by AvailabilityId
    public async Task<(Availability?, OperationStatus)> getAvailabilityById(int availabilityId)
    {
        try
        {
            var availability = await _db.Availability.FindAsync(availabilityId);
            return (availability, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from getAvailabilityById(): \n" +
                             "Something went wrong when retreiving Availability where " +
                            $"AvailabilityId = {availabilityId}, Error message: {e}");
            return (null, OperationStatus.Error);
        }
    }

    // method for retreiving range of Availability with list of AvailabilityIds
    public async 
        Task<(List<Availability>, OperationStatus)> getAvailabilityByIds(int[] availabilityIds)
    {
        try
        {
            var availabilityRange = await _db.Availability
                .Where(a => availabilityIds.Contains(a.AvailabilityId))
                .ToListAsync();
            return (availabilityRange, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all AvailabilityIds
            var availabilityIdsString = String.Join(", ", availabilityIds);

            _logger.LogError("[AvailabilityRepo] Error from getAvailabilityByIds(): \n" +
                             "Something went wrong when retreiving range of Availability " +
                            $"with list of AvailabilityIds {availabilityIdsString}, " + 
                            $"Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }


    // method for retreiving Availability by DayOfWeek and From properties
    public async Task<(List<Availability>, OperationStatus)> 
        getAvailabilityByDoW(DayOfWeek dayOfWeek, TimeOnly from)
    {
        try
        {
            var availability = await _db.Availability
                .Where(a => a.DayOfWeek == dayOfWeek && a.From == from)
                .ToListAsync();
            return (availability, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from getAvailabilityByDoW(): \n" +
                             "Something went wrong when retreiving Availability where " +
                            $"DayOfWeek = {dayOfWeek} and From = {from}, Error message: {e}");
            return ([], OperationStatus.Error);
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
                            $"UserId = {userId} and Date = null, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method for retreiving a Worker's Availability for the week where Date is not null
    public async Task<(List<Availability>, OperationStatus)> 
        getWeeksDateAvailability(string userId, DateOnly monday, DateOnly sunday)
    {
        try
        {
            // retreives list of availability between given dates for monday and sunday
            var availability = await _db.Availability
                .Where(a => a.UserId == userId && a.Date != null && 
                       a.Date >= monday && a.Date <= sunday)
                .ToListAsync();
            return (availability, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from getWeeksDateAvailability(): \n" +
                             "Something went wrong when retreiving Availability where " +
                            $"UserId = {userId}, and Date is between {monday} and {sunday}, " +
                            $"Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }

    // method for retreiving a Worker's Availability for timeslot where Date is null
    public async Task<(List<Availability>, OperationStatus)> 
        getTimeslotsDoWAvailability(string userId, DayOfWeek dayOfWeek, TimeOnly from, TimeOnly to)
    {
        try
        {
            // retreives list of availability for given dayOfWeek between given from and to
            // Get all 30-minute slots that fall within the requested time range
            var availability = await _db.Availability
                .Where(a => a.UserId == userId && a.DayOfWeek == dayOfWeek && 
                       a.Date == null && a.From >= from && a.From < to)
                .ToListAsync();
            return (availability, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from getTimeslotsDoWAvailability(): \n" +
                             "Something went wrong when retreiving range of Availability " +
                            $"between {from} and {to} where UserId = {userId}, DayOfWeek = " + 
                            $"{dayOfWeek} and Date = null, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }
    
    // method for retreiving a Worker's Availability for timeslot where Date is not null
    public async Task<(List<Availability>, OperationStatus)> 
        getTimeslotsDateAvailability(string userId, DateOnly date, TimeOnly from, TimeOnly to)
    {
        try
        {
            // retreives list of availability for given date between given from and to
            // Get all 30-minute slots that fall within the requested time range
            var availability = await _db.Availability
                .Where(a => a.UserId == userId && a.Date != null &&
                       a.Date == date && a.From >= from && a.From < to)
                .ToListAsync();
            return (availability, OperationStatus.Ok);
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from getTimeslotsDateAvailability(): \n" +
                             "Something went wrong when retreiving range of Availability " +
                            $"between {from} and {to} where UserId = {userId} and Date = " + 
                            $"{date}, Error message: {e}");
            return ([], OperationStatus.Error);
        }
    }


    // CREATE FUNCTIONS:

    // method for adding Availability into table
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


    // DELETE FUNCTIONS:

    // method for deleting Availability from table
    public async Task<OperationStatus> deleteAvailability(Availability availability)
    {
        try 
        {
            _db.Remove(availability);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            _logger.LogError("[AvailabilityRepo] Error from deleteAvailability(): \n" +
                             "Something went wrong when deleting Availability " +
                            $"{@availability}, Error message: {e}");
            return OperationStatus.Error;
        }
    }

    // method for deleting range of Availability from table
    public async Task<OperationStatus> deleteAvailabilityRange(List<Availability> availabilityRange)
    {
        try 
        {
            _db.RemoveRange(availabilityRange);
            await _db.SaveChangesAsync();
            return OperationStatus.Ok;
        }
        catch (Exception e) // In case of unexpected exception
        {
            // makes string listing all Availability
            var availabilityStrings = availabilityRange.ConvertAll(a => $"{@a}");
            var availabilityRangeString = String.Join(", ", availabilityStrings);
            
            _logger.LogError("[AvailabilityRepo] Error from deleteAvailabilityRange(): \n" +
                             "Something went wrong when deleting range of Availability " +
                            $"{availabilityRangeString}, Error message: {e}");
            return OperationStatus.Error;
        }
    }
}
