using HealthCalendar.DTOs;
using HealthCalendar.Models;
using HealthCalendar.Shared;

namespace HealthCalendar.DAL;

public interface IAvailabilityRepo
{
    Task<(Availability? availability, OperationStatus)> getAvailabilityById(int availabilityId);
    Task<(List<Availability>, OperationStatus)> getWeeksDoWAvailability(string userId);
    Task<(List<Availability>, OperationStatus)> getWeeksDateAvailability(string userId, DateOnly monday, DateOnly sunday);
    Task<OperationStatus> createAvailability(Availability availability);
    Task<OperationStatus> deleteAvailability(Availability availability);
}