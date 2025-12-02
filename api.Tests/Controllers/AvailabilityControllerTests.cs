using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using HealthCalendar.DAL;
using HealthCalendar.Controllers;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using HealthCalendar.DTOs;
using System.Threading.Tasks;

public class AvailabilityControllerTests
{
    [Fact]
    public async Task getWeeksAvailabilityProper()
    {
        // Given
        var userId = "id1";
        var monday = new DateOnly(2025, 12, 29);
        var sunday = monday.AddDays(6);

        var weeksDowAvailability = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 1,
                From = new TimeOnly(10,30),
                To = new TimeOnly(11,0),
                DayOfWeek = DayOfWeek.Monday,
                Date = null,
                UserId = "id1"
            },
            new Availability
            {
                AvailabilityId = 2,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Monday,
                Date = null,
                UserId = "id1"
            },
            new Availability
            {
                AvailabilityId = 3,
                From = new TimeOnly(10,30),
                To = new TimeOnly(11,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id1"
            },
            new Availability
            {
                AvailabilityId = 4,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id1"
            }
        };

        var weeksDateAvailability = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 5,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = new DateOnly(2025, 12, 30),
                UserId = "id1"
            },
            new Availability
            {
                AvailabilityId = 6,
                From = new TimeOnly(11,30),
                To = new TimeOnly(12,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = new DateOnly(2025, 12, 30),
                UserId = "id1"
            }
        };

        var weeksAvailabilityDTOs = new List<AvailabilityDTO> {
            new AvailabilityDTO
            {
                AvailabilityId = 1,
                From = new TimeOnly(10,30),
                To = new TimeOnly(11,0),
                DayOfWeek = DayOfWeek.Monday,
                Date = null,
                UserId = "id1"
            },
            new AvailabilityDTO
            {
                AvailabilityId = 2,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Monday,
                Date = null,
                UserId = "id1"
            },
            new AvailabilityDTO
            {
                AvailabilityId = 3,
                From = new TimeOnly(10,30),
                To = new TimeOnly(11,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id1"
            },
            new AvailabilityDTO
            {
                AvailabilityId = 6,
                From = new TimeOnly(11,30),
                To = new TimeOnly(12,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = new DateOnly(2025, 12, 30),
                UserId = "id1"
            }
        };

        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        mockAvailabilityRepo
            .Setup(repo => repo.getWeeksDoWAvailability(userId))
            .ReturnsAsync((weeksDowAvailability, OperationStatus.Ok));
        mockAvailabilityRepo
            .Setup(repo => repo.getWeeksDateAvailability(userId, monday, sunday))
            .ReturnsAsync((weeksDateAvailability, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<AvailabilityController>>();
        var availabilityController = new AvailabilityController(mockAvailabilityRepo.Object, mockLogger.Object);

        // When
        var result = await availabilityController.getWeeksAvailabilityProper(userId, monday);
    
        // Then
        var viewResult = Assert.IsType<OkObjectResult>(result);
        var viewAvailabilityDTOs = Assert.IsAssignableFrom<IEnumerable<AvailabilityDTO>>(viewResult.Value).ToList();
        Assert.Equal(4, viewAvailabilityDTOs.Count());
        Assert.Equal(
            weeksAvailabilityDTOs.Select(a => (a.AvailabilityId, a.From, a.To, a.DayOfWeek, a.Date, a.UserId)), 
            viewAvailabilityDTOs.Select(a => (a.AvailabilityId, a.From, a.To, a.DayOfWeek, a.Date, a.UserId))
        );
    }
}