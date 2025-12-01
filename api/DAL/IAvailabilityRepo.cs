using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IAvailabilityRepo
{
    Task<(Availability?, OperationStatus)> getAvailabilityById(int availabilityId);
    Task<(List<Availability>, OperationStatus)> getAvailabilityByIds(int[] availabilityIds);
    Task<(List<Availability>, OperationStatus)> getAvailabilityByUserId(string userId);
    Task<(Availability?, OperationStatus)> getAvailabilityByDoW(string userId, DayOfWeek dayOfWeek, TimeOnly from);
    Task<(List<Availability>, OperationStatus)> getAvailabilityRangeByDoW(string userId, DayOfWeek dayOfWeek, TimeOnly from);
    Task<(List<Availability>, OperationStatus)> getWeeksDoWAvailability(string userId);
    Task<(List<Availability>, OperationStatus)> getWeeksDateAvailability(string userId, DateOnly monday, DateOnly sunday);
    
    Task<(List<Availability>, OperationStatus)> 
        getTimeslotsDoWAvailability(string userId, DayOfWeek dayOfWeek, TimeOnly from, TimeOnly to);
    
    Task<(List<Availability>, OperationStatus)> 
        getTimeslotsDateAvailability(string userId, DateOnly date, TimeOnly from, TimeOnly to);
    Task<OperationStatus> createAvailability(Availability availability);
    Task<OperationStatus> deleteAvailability(Availability availability);
    Task<OperationStatus> deleteAvailabilityRange(List<Availability> availabilityRange);
}