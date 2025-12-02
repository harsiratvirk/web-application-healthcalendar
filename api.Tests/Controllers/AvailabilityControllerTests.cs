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
    // Tests if function filters out overlapping doWAvailability and dateAvailability
    [Fact]
    public async Task TestGetWeeksAvailabilityProper()
    {
        // Arrange
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

        // Act
        var result = await availabilityController.getWeeksAvailabilityProper(userId, monday);
    
        // Assert
        var viewResult = Assert.IsType<OkObjectResult>(result);
        var viewAvailabilityDTOs = Assert.IsAssignableFrom<IEnumerable<AvailabilityDTO>>(viewResult.Value).ToList();
        Assert.Equal(4, viewAvailabilityDTOs.Count());
        Assert.Equal(
            weeksAvailabilityDTOs.Select(a => (a.AvailabilityId, a.From, a.To, a.DayOfWeek, a.Date, a.UserId)), 
            viewAvailabilityDTOs.Select(a => (a.AvailabilityId, a.From, a.To, a.DayOfWeek, a.Date, a.UserId))
        );
    }

    // Tests what happens if relevant Availability is not continuous when creating Event
    [Fact]
    public async Task TestCheckAvailabilityForCreateNotContinuous()
    {
        // Arrange
        var eventDTO = new EventDTO
        {
            From = new TimeOnly(10,30),
            To = new TimeOnly(12,0),
            Date = new DateOnly(2025, 12, 30),
            Title = "Medication reminder",
            Location = "Home",
            UserId = "id1"
        };
        var userId = "id2";
        var dayOfWeek = DayOfWeek.Tuesday;
        var date = new DateOnly(2025, 12, 30);
        var from = new TimeOnly(10,30);
        var to = new TimeOnly(12,0);

        var doWAvailabilityRange = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 1,
                From = new TimeOnly(10,30),
                To = new TimeOnly(11,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id2"
            },
            new Availability
            {
                AvailabilityId = 2,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id2"
            }
        };

        var dateAvailabilityRange = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 3,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = new DateOnly(2025, 12, 30),
                UserId = "id2"
            },
            new Availability
            {
                AvailabilityId = 4,
                From = new TimeOnly(11,30),
                To = new TimeOnly(12,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = new DateOnly(2025, 12, 30),
                UserId = "id2"
            }
        };

        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDoWAvailability(userId, dayOfWeek, from, to))
            .ReturnsAsync((doWAvailabilityRange, OperationStatus.Ok));
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDateAvailability(userId, date, from, to))
            .ReturnsAsync((dateAvailabilityRange, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<AvailabilityController>>();
        var availabilityController = new AvailabilityController(mockAvailabilityRepo.Object, mockLogger.Object);

        // Act
        var result = await availabilityController.checkAvailabilityForCreate(eventDTO, userId);
    
        // Assert
        var viewResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(406, viewResult.StatusCode);
    }

    // Test for when relevant Availability is continuous when updating Event
    [Fact]
    public async Task TestCheckAvailabilityForUpdate()
    {
        // Arrange
        var updatedEventDTO = new EventDTO
        {
            From = new TimeOnly(10,0),
            To = new TimeOnly(12,30),
            Date = new DateOnly(2025, 12, 30),
            Title = "Medication reminder",
            Location = "Home",
            UserId = "id1"
        };
        
        var userId = "id2";
        var dayOfWeek = DayOfWeek.Tuesday;
        var date = new DateOnly(2025, 12, 30);

        var updatedFrom = new TimeOnly(10,0);
        var updatedTo = new TimeOnly(12,30);
        var oldFrom = new TimeOnly(11,30);
        var oldTo = new TimeOnly(13,30);

        // Availability for createSchedules
        var doWAvailabilityForCreate = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 1,
                From = new TimeOnly(10,0),
                To = new TimeOnly(10,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id2"
            },
            new Availability
            {
                AvailabilityId = 2,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id2"
            },
        };
        var dateAvailabilityForCreate = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 6,
                From = new TimeOnly(10,30),
                To = new TimeOnly(11,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = new DateOnly(2025, 12, 30),
                UserId = "id2"
            }
        };
        // Availability for deleteSchedules
        var doWAvailabilityForDelete = new List<Availability>
        {
            new Availability
            {
                AvailabilityId = 3,
                From = new TimeOnly(11,30),
                To = new TimeOnly(12,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id2"
            },
            new Availability
            {
                AvailabilityId = 4,
                From = new TimeOnly(12,0),
                To = new TimeOnly(12,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id2"
            }
        };
        var dateAvailabilityForDelete = new List<Availability>();
        // Availability for updateSchedules
        var doWAvailabilityForUpdate = new List<Availability>
        {
            new Availability
            {
                AvailabilityId = 5,
                From = new TimeOnly(12,30),
                To = new TimeOnly(13,0),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = null,
                UserId = "id2"
            }
        };
        var dateAvailabilityForUpdate = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 7,
                From = new TimeOnly(13,0),
                To = new TimeOnly(13,30),
                DayOfWeek = DayOfWeek.Tuesday,
                Date = new DateOnly(2025, 12, 30),
                UserId = "id2"
            }
        };

        var eventUpdateListsDTO = new EventUpdateListsDTO
        {
            ForCreateSchedules = [1, 6, 2],
            ForDeleteSchedules = [3, 4],
            ForUpdateSchedules = [5, 7]
        };

        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        // When retreiving Availability for createSchedules
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDoWAvailability(userId, dayOfWeek, updatedFrom, oldFrom))
            .ReturnsAsync((doWAvailabilityForCreate, OperationStatus.Ok));
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDateAvailability(userId, date, updatedFrom, oldFrom))
            .ReturnsAsync((dateAvailabilityForCreate, OperationStatus.Ok));
        // When retreiving Availability for deleteSchedules
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDoWAvailability(userId, dayOfWeek, updatedTo, oldTo))
            .ReturnsAsync((doWAvailabilityForDelete, OperationStatus.Ok));
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDateAvailability(userId, date, updatedTo, oldTo))
            .ReturnsAsync((dateAvailabilityForDelete, OperationStatus.Ok));
        // When retreiving Availability for updateSchedules
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDoWAvailability(userId, dayOfWeek, oldFrom, updatedTo))
            .ReturnsAsync((doWAvailabilityForUpdate, OperationStatus.Ok));
        mockAvailabilityRepo
            .Setup(repo => repo.getTimeslotsDateAvailability(userId, date, oldFrom, updatedTo))
            .ReturnsAsync((dateAvailabilityForUpdate, OperationStatus.Ok));
        
        var mockLogger = new Mock<ILogger<AvailabilityController>>();
        var availabilityController = new AvailabilityController(mockAvailabilityRepo.Object, mockLogger.Object);

        // Act
        var result = await availabilityController
            .checkAvailabilityForUpdate(updatedEventDTO, date, oldFrom, oldTo, userId);
    
        // Assert
        var viewResult = Assert.IsType<OkObjectResult>(result);
        var viewEventUpdateListsDTO = Assert.IsAssignableFrom<EventUpdateListsDTO>(viewResult.Value);
        Assert.Equal(3, viewEventUpdateListsDTO.ForCreateSchedules.Count());
        Assert.Equal(2, viewEventUpdateListsDTO.ForDeleteSchedules.Count());
        Assert.Equal(2, viewEventUpdateListsDTO.ForUpdateSchedules.Count());
        Assert.Equal(eventUpdateListsDTO.ForCreateSchedules, viewEventUpdateListsDTO.ForCreateSchedules);
        Assert.Equal(eventUpdateListsDTO.ForDeleteSchedules, viewEventUpdateListsDTO.ForDeleteSchedules);
        Assert.Equal(eventUpdateListsDTO.ForUpdateSchedules, viewEventUpdateListsDTO.ForUpdateSchedules);

    }
}