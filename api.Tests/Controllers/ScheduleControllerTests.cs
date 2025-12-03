using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using HealthCalendar.DAL;
using HealthCalendar.Controllers;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using HealthCalendar.DTOs;
using System.Threading.Tasks;

public class ScheduleControllerTests
{
    // Test retreival of EventIds of Events related to specific Availability
    [Fact]
    public async Task TestGetScheduledEventIds()
    {
        // Arrange

        var availabilityId = 1;

        var schedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 1,
                EventId = 1
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2026, 1, 5),
                AvailabilityId = 1,
                EventId = 2
            },
            new Schedule
            {
                ScheduleId = 3,
                Date = new DateOnly(2026, 1, 12),
                AvailabilityId = 1,
                EventId = 3
            }
        };

        int[] eventIds = [1, 2, 3];


        var mockScheduleRepo = new Mock<IScheduleRepo>();
        mockScheduleRepo
            .Setup(repo => repo.getSchedulesByAvailabilityId(availabilityId))
            .ReturnsAsync((schedules, OperationStatus.Ok));
        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        var mockEventRepo = new Mock<IEventRepo>();
        var mockLogger = new Mock<ILogger<ScheduleController>>();
        var scheduleController = new ScheduleController(mockScheduleRepo.Object, mockAvailabilityRepo.Object,
                                                        mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await scheduleController.getScheduledEventIds(availabilityId);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var eventIdsResult = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value).ToArray();
        Assert.Equal(3, eventIdsResult.Count());
        Assert.Equal(eventIds, eventIdsResult);
    }

    // Test retreival of EventId for Events related to specific Availability on specific date
    [Fact]
    public async Task TestFindScheduledEventId()
    {
        // Arrange

        var availabilityId = 1;
        var date = new DateOnly(2026, 1, 5);

        var schedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 1,
                EventId = 1
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2026, 1, 5),
                AvailabilityId = 1,
                EventId = 2
            },
            new Schedule
            {
                ScheduleId = 3,
                Date = new DateOnly(2026, 1, 12),
                AvailabilityId = 1,
                EventId = 3
            }
        };

        int eventId = 2;


        var mockScheduleRepo = new Mock<IScheduleRepo>();
        mockScheduleRepo
            .Setup(repo => repo.getSchedulesByAvailabilityId(availabilityId))
            .ReturnsAsync((schedules, OperationStatus.Ok));
        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        var mockEventRepo = new Mock<IEventRepo>();
        var mockLogger = new Mock<ILogger<ScheduleController>>();
        var scheduleController = new ScheduleController(mockScheduleRepo.Object, mockAvailabilityRepo.Object,
                                                        mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await scheduleController.findScheduledEventId(availabilityId, date);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var eventIdResult = Assert.IsAssignableFrom<int>(okResult.Value);
        Assert.Equal(eventId, eventIdResult);
    }

    // Test for Error occuring when creating Schedules
    [Fact]
    public async Task TestCreateSchedulesNotOk()
    {
        // Arrange

        int[] availabilityIds = [1, 2, 3];
        int eventId = 1;
        var date = new DateOnly(2025, 12, 29);

        var eventt = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 00),
            To = new TimeOnly(11, 30),
            Date = new DateOnly(2025, 12, 29),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var availabilityRange = new List<Availability>() {
            new Availability
            {
                AvailabilityId = 1,
                From = new TimeOnly(10,0),
                To = new TimeOnly(10,30),
                DayOfWeek = DayOfWeek.Monday,
                Date = null,
                UserId = "id2"
            },
            new Availability
            {
                AvailabilityId = 2,
                From = new TimeOnly(10,30),
                To = new TimeOnly(11,0),
                DayOfWeek = DayOfWeek.Monday,
                Date = new DateOnly(2025, 12, 29),
                UserId = "id2"
            },
            new Availability
            {
                AvailabilityId = 3,
                From = new TimeOnly(11,0),
                To = new TimeOnly(11,30),
                DayOfWeek = DayOfWeek.Monday,
                Date = new DateOnly(2025, 12, 29),
                UserId = "id2"
            },
        };

        var schedules = new List<Schedule> {
            new Schedule
            {
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 1,
                Availability = availabilityRange[0],
                EventId = 1,
                Event = eventt
            },
            new Schedule
            {
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 2,
                Availability = availabilityRange[1],
                EventId = 1,
                Event = eventt
            },
            new Schedule
            {
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 3,
                Availability = availabilityRange[2],
                EventId = 1,
                Event = eventt
            }
        };

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getEventById(eventId))
            .ReturnsAsync((eventt, OperationStatus.Ok));

        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        mockAvailabilityRepo
            .Setup(repo => repo.getAvailabilityByIds(availabilityIds))
            .ReturnsAsync((availabilityRange, OperationStatus.Ok));

        var mockScheduleRepo = new Mock<IScheduleRepo>();
        mockScheduleRepo
            .Setup(repo => repo.createSchedules(
                It.Is<List<Schedule>>(list =>
                    list.Count == schedules.Count &&
                    list.All(s1 =>
                        schedules.Any(s2 =>
                            s2.Date == s1.Date &&
                            s2.AvailabilityId == s1.AvailabilityId &&
                            s2.Availability == s1.Availability &&
                            s2.EventId == s1.EventId &&
                            s2.Event == s1.Event
                        )
                    )
                )
            )).ReturnsAsync(OperationStatus.Error);

        var mockLogger = new Mock<ILogger<ScheduleController>>();
        var scheduleController = new ScheduleController(mockScheduleRepo.Object, mockAvailabilityRepo.Object,
                                                        mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await scheduleController.createSchedules(availabilityIds, eventId, date);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    // Test for Updating Availability of Schedules
    [Fact]
    public async Task TestUpdateScheduledAvailability()
    {
        // Arrange

        int[] oldAvailabilityIds = [1, 2];
        var newAvailabilityId = 3;

        var newAvailability = new Availability
        {
            AvailabilityId = 3,
            From = new TimeOnly(10,30),
            To = new TimeOnly(11,0),
            DayOfWeek = DayOfWeek.Monday,
            Date = null,
            UserId = "id1"
        };

        var oldAvailability1 = new Availability
        {
            AvailabilityId = 1,
            From = new TimeOnly(10,30),
            To = new TimeOnly(11,0),
            DayOfWeek = DayOfWeek.Monday,
            Date = new DateOnly(2025, 12, 29),
            UserId = "id1"
        };
        var oldAvailability2 = new Availability
        {
            AvailabilityId = 2,
            From = new TimeOnly(10,30),
            To = new TimeOnly(11,0),
            DayOfWeek = DayOfWeek.Monday,
            Date = new DateOnly(2026, 1, 5),
            UserId = "id1"
        };

        var event1 = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 00),
            To = new TimeOnly(11, 30),
            Date = new DateOnly(2025, 12, 29),
            Title = "Take medication",
            Location = "Home",
            UserId = "id2"
        };

        var event2 = new Event
        {
            EventId = 2,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(13, 0),
            Date = new DateOnly(2026, 1, 5),
            Title = "Go for a walk",
            Location = "The park",
            UserId = "id3"
        };

        var oldSchedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 1,
                Availability = oldAvailability1,
                EventId = 1,
                Event = event1
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2026, 1, 5),
                AvailabilityId = 2,
                Availability = oldAvailability2,
                EventId = 2,
                Event = event2
            }
        };

        var updatedSchedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 3,
                Availability = newAvailability,
                EventId = 1,
                Event = event1
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2026, 1, 5),
                AvailabilityId = 3,
                Availability = newAvailability,
                EventId = 2,
                Event = event2
            }
        };
        

        var mockEventRepo = new Mock<IEventRepo>();

        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        mockAvailabilityRepo
            .Setup(repo => repo.getAvailabilityById(newAvailabilityId))
            .ReturnsAsync((newAvailability, OperationStatus.Ok));

        var mockScheduleRepo = new Mock<IScheduleRepo>();
        mockScheduleRepo
            .Setup(repo => repo.getSchedulesByAvailabilityIds(oldAvailabilityIds))
            .ReturnsAsync((oldSchedules, OperationStatus.Ok));
        mockScheduleRepo
            .Setup(repo => repo.updateSchedules(
                It.Is<List<Schedule>>(list =>
                    list.Count == updatedSchedules.Count &&
                    list.All(s1 =>
                        updatedSchedules.Any(s2 =>
                            s2.ScheduleId == s1.ScheduleId &&
                            s2.Date == s1.Date &&
                            s2.AvailabilityId == s1.AvailabilityId &&
                            s2.Availability == s1.Availability &&
                            s2.EventId == s1.EventId &&
                            s2.Event == s1.Event
                        )
                    )
                )
            )).ReturnsAsync(OperationStatus.Ok);

        var mockLogger = new Mock<ILogger<ScheduleController>>();
        var scheduleController = new ScheduleController(mockScheduleRepo.Object, mockAvailabilityRepo.Object,
                                                        mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await scheduleController.updateScheduledAvailability(oldAvailabilityIds, newAvailabilityId);
    
        // Assert
        var errorResult = Assert.IsType<OkObjectResult>(result);
    }

    // Test for Error occuring when Updating Availability of Schedules
    [Fact]
    public async Task TestUpdateScheduledEventNotOk()
    {
        // Arrange

        var eventId = 1;
        int[] availabilityIds = [1, 2];

        var updatedEvent = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 00),
            To = new TimeOnly(11, 30),
            Date = new DateOnly(2025, 12, 29),
            Title = "Take medication",
            Location = "Home",
            UserId = "id2"
        };

        var oldEvent = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2025, 12, 29),
            Title = "Take medication",
            Location = "Home",
            UserId = "id2"
        };

        var availability1 = new Availability
        {
            AvailabilityId = 1,
            From = new TimeOnly(10,30),
            To = new TimeOnly(11,0),
            DayOfWeek = DayOfWeek.Monday,
            Date = new DateOnly(2025, 12, 29),
            UserId = "id1"
        };
        var availability2 = new Availability
        {
            AvailabilityId = 2,
            From = new TimeOnly(11,0),
            To = new TimeOnly(11,30),
            DayOfWeek = DayOfWeek.Monday,
            Date = null,
            UserId = "id1"
        };

        var oldSchedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 1,
                Availability = availability1,
                EventId = 1,
                Event = oldEvent
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 2,
                Availability = availability2,
                EventId = 1,
                Event = oldEvent
            }
        };

        var updatedSchedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 1,
                Availability = availability1,
                EventId = 1,
                Event = updatedEvent
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 2,
                Availability = availability2,
                EventId = 1,
                Event = updatedEvent
            }
        };


        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getEventById(eventId))
            .ReturnsAsync((updatedEvent, OperationStatus.Ok));

        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();

        var mockScheduleRepo = new Mock<IScheduleRepo>();
        mockScheduleRepo
            .Setup(repo => repo.getSchedulesAfterEventUpdate(eventId, availabilityIds))
            .ReturnsAsync((oldSchedules, OperationStatus.Ok));
        mockScheduleRepo
            .Setup(repo => repo.updateSchedules(
                It.Is<List<Schedule>>(list =>
                    list.Count == updatedSchedules.Count &&
                    list.All(s1 =>
                        updatedSchedules.Any(s2 =>
                            s2.ScheduleId == s1.ScheduleId &&
                            s2.Date == s1.Date &&
                            s2.AvailabilityId == s1.AvailabilityId &&
                            s2.Availability == s1.Availability &&
                            s2.EventId == s1.EventId &&
                            s2.Event == s1.Event
                        )
                    )
                )
            )).ReturnsAsync(OperationStatus.Error);

        var mockLogger = new Mock<ILogger<ScheduleController>>();
        var scheduleController = new ScheduleController(mockScheduleRepo.Object, mockAvailabilityRepo.Object,
                                                        mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await scheduleController.updateScheduledEvent(eventId, availabilityIds);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    // Test for Error occuring when deleting Schedules
    [Fact]
    public async Task TestDeleteSchedulesByEventIdNotOk()
    {
        // Arrange

        var eventId = 1;

        var eventt = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2025, 12, 29),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var availability1 = new Availability
        {
            AvailabilityId = 1,
            From = new TimeOnly(10,30),
            To = new TimeOnly(11,0),
            DayOfWeek = DayOfWeek.Monday,
            Date = new DateOnly(2025, 12, 29),
            UserId = "id2"
        };
        var availability2 = new Availability
        {
            AvailabilityId = 2,
            From = new TimeOnly(11,0),
            To = new TimeOnly(11,30),
            DayOfWeek = DayOfWeek.Monday,
            Date = null,
            UserId = "id2"
        };
        var availability3 = new Availability
        {
            AvailabilityId = 3,
            From = new TimeOnly(11,30),
            To = new TimeOnly(12,0),
            DayOfWeek = DayOfWeek.Monday,
            Date = null,
            UserId = "id2"
        };

        var schedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 1,
                Availability = availability1,
                EventId = 1,
                Event = eventt
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 2,
                Availability = availability2,
                EventId = 1,
                Event = eventt
            },
            new Schedule
            {
                ScheduleId = 3,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 3,
                Availability = availability3,
                EventId = 1,
                Event = eventt
            }
        };

        var mockEventRepo = new Mock<IEventRepo>();
        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        var mockScheduleRepo = new Mock<IScheduleRepo>();
        mockScheduleRepo
            .Setup(repo => repo.getSchedulesByEventId(eventId))
            .ReturnsAsync((schedules, OperationStatus.Ok));
        mockScheduleRepo
            .Setup(repo => repo.deleteSchedules(schedules))
            .ReturnsAsync(OperationStatus.Error);

        var mockLogger = new Mock<ILogger<ScheduleController>>();
        var scheduleController = new ScheduleController(mockScheduleRepo.Object, mockAvailabilityRepo.Object,
                                                        mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await scheduleController.deleteSchedulesByEventId(eventId);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }
    
    // Test for deleting list of Schedules
    [Fact]
    public async Task TestDeleteSchedulesAfterEventUpdate()
    {
        // Arrange

        var eventId = 1;
        int[] availabilityIds = [3, 4];

        var eventt = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2025, 12, 29),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var availability1 = new Availability
        {
            AvailabilityId = 3,
            From = new TimeOnly(11,30),
            To = new TimeOnly(12,0),
            DayOfWeek = DayOfWeek.Monday,
            Date = null,
            UserId = "id2"
        };
        var availability2 = new Availability
        {
            AvailabilityId = 4,
            From = new TimeOnly(12,0),
            To = new TimeOnly(12,30),
            DayOfWeek = DayOfWeek.Monday,
            Date = null,
            UserId = "id2"
        };

        var schedules = new List<Schedule> {
            new Schedule
            {
                ScheduleId = 1,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 3,
                Availability = availability1,
                EventId = 1,
                Event = eventt
            },
            new Schedule
            {
                ScheduleId = 2,
                Date = new DateOnly(2025, 12, 29),
                AvailabilityId = 4,
                Availability = availability2,
                EventId = 1,
                Event = eventt
            }
        };

        var mockEventRepo = new Mock<IEventRepo>();
        var mockAvailabilityRepo = new Mock<IAvailabilityRepo>();
        var mockScheduleRepo = new Mock<IScheduleRepo>();
        mockScheduleRepo
            .Setup(repo => repo.getSchedulesAfterEventUpdate(eventId, availabilityIds))
            .ReturnsAsync((schedules, OperationStatus.Ok));
        mockScheduleRepo
            .Setup(repo => repo.deleteSchedules(schedules))
            .ReturnsAsync(OperationStatus.Ok);

        var mockLogger = new Mock<ILogger<ScheduleController>>();
        var scheduleController = new ScheduleController(mockScheduleRepo.Object, mockAvailabilityRepo.Object,
                                                        mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await scheduleController.deleteSchedulesAfterEventUpdate(eventId, availabilityIds);
    
        // Assert
        var errorResult = Assert.IsType<OkObjectResult>(result);
    }
}