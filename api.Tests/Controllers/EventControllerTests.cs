using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using HealthCalendar.DAL;
using HealthCalendar.Controllers;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using HealthCalendar.DTOs;
using System.Threading.Tasks;

public class EventControllerTests
{
    // Test for when error occurs while retreiving an Event
    [Fact]
    public async Task TestGetEventNotOk()
    {
        // Arrange
        var eventId = 1;

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getEventById(eventId))
            .ReturnsAsync((null, OperationStatus.Error));
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.getEvent(eventId);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    // Test for function where list of Events are retreived
    [Fact]
    public async Task TestGetWeeksEventsByUserIds()
    {
        // Arrange

        string[] userIds = ["id1", "id2", "id3"];
        var monday = new DateOnly(2026, 1, 5);
        var sunday = monday.AddDays(6);

        var events = new List<Event>
        {
            new Event
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1"
            },
            new Event
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 6),
                Title = "Excercise",
                Location = "Home",
                UserId = "id2"
            },
            new Event
            {
                EventId = 3,
                From = new TimeOnly(9, 0),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 8),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id2"
            },
            new Event
            {
                EventId = 4,
                From = new TimeOnly(11, 0),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 7),
                Title = "Shop for groceries",
                Location = "The mall",
                UserId = "id3"
            }
        };

        var eventDTOs = new List<EventDTO>
        {
            new EventDTO
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1"
            },
            new EventDTO
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 6),
                Title = "Excercise",
                Location = "Home",
                UserId = "id2"
            },
            new EventDTO
            {
                EventId = 3,
                From = new TimeOnly(9, 0),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 8),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id2"
            },
            new EventDTO
            {
                EventId = 4,
                From = new TimeOnly(11, 0),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 7),
                Title = "Shop for groceries",
                Location = "The mall",
                UserId = "id3"
            }
        };


        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getWeeksEventsByUserIds(userIds, monday, sunday))
            .ReturnsAsync((events, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.getWeeksEventsByUserIds(userIds, monday);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var eventDTOsResult = Assert.IsAssignableFrom<IEnumerable<EventDTO>>(okResult.Value).ToList();
        Assert.Equal(4, eventDTOsResult.Count());
        Assert.Equal(
            eventDTOs.Select(dto => (dto.EventId, dto.From, dto.To, dto.Date, dto.Title, dto.Location, dto.UserId)), 
            eventDTOsResult.Select(dto => (dto.EventId, dto.From, dto.To, dto.Date, dto.Title, dto.Location, dto.UserId))
        );
    }

    // Test for function where Events for the week are retreived for a Worker
    [Fact]
    public async Task TestGetWeeksEventsForWorker()
    {
        // Arrange

        var monday = new DateOnly(2026, 1, 5);
        var sunday = monday.AddDays(6);

        var patients = new List<UserDTO>
        {
            new UserDTO
            {
                Id = "id1",
                UserName = "bob@gmail.com",
                Name = "Bob",
                Role = Roles.Patient,
                WorkerId = "id4"
            },
            new UserDTO
            {
                Id = "id2",
                UserName = "lars@gmail.com",
                Name = "Lars",
                Role = Roles.Patient,
                WorkerId = "id4"
            },
            new UserDTO
            {
                Id = "id3",
                UserName = "karl@gmail.com",
                Name = "Karl",
                Role = Roles.Patient,
                WorkerId = "id4"
            }
        };

        var patient1Id = "id1";
        var patient2Id = "id2";
        var patient3Id = "id3";

        var patient1Events = new List<Event>
        {
            new Event
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1"
            }
        };

        var patient2Events = new List<Event>
        {
            new Event
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 6),
                Title = "Excercise",
                Location = "Home",
                UserId = "id2"
            },
            new Event
            {
                EventId = 3,
                From = new TimeOnly(9, 0),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 8),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id2"
            }
        };

        var patient3Events = new List<Event>
        {
            new Event
            {
                EventId = 4,
                From = new TimeOnly(11, 0),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 7),
                Title = "Shop for groceries",
                Location = "The mall",
                UserId = "id3"
            }
        };

        var eventsWithOwnerDTOs = new List<EventWithOwnerDTO>
        {
            new EventWithOwnerDTO
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1",
                OwnerName = "Bob"
            },
            new EventWithOwnerDTO
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 6),
                Title = "Excercise",
                Location = "Home",
                UserId = "id2",
                OwnerName = "Lars"
            },
            new EventWithOwnerDTO
            {
                EventId = 3,
                From = new TimeOnly(9, 0),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 8),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id2",
                OwnerName = "Lars"
            },
            new EventWithOwnerDTO
            {
                EventId = 4,
                From = new TimeOnly(11, 0),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 7),
                Title = "Shop for groceries",
                Location = "The mall",
                UserId = "id3",
                OwnerName = "Karl"
            }
        };


        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getWeeksEventsByUserId(patient1Id, monday, sunday))
            .ReturnsAsync((patient1Events, OperationStatus.Ok));
        mockEventRepo
            .Setup(repo => repo.getWeeksEventsByUserId(patient2Id, monday, sunday))
            .ReturnsAsync((patient2Events, OperationStatus.Ok));
        mockEventRepo
            .Setup(repo => repo.getWeeksEventsByUserId(patient3Id, monday, sunday))
            .ReturnsAsync((patient3Events, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.getWeeksEventsForWorker(patients, monday);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var eventsWithOwnerDTOsResult = Assert.IsAssignableFrom<IEnumerable<EventWithOwnerDTO>>(okResult.Value).ToList();
        Assert.Equal(4, eventsWithOwnerDTOsResult.Count());
        Assert.Equal(
            eventsWithOwnerDTOs.Select(dto => (
                dto.EventId, dto.From, dto.To, dto.Date, dto.Title, dto.Location, dto.UserId, dto.OwnerName
            )), 
            eventsWithOwnerDTOsResult.Select(dto => (
                dto.EventId, dto.From, dto.To, dto.Date, dto.Title, dto.Location, dto.UserId, dto.OwnerName
            ))
        );
    }

    // Test for function where list of EventIds are retreived
    [Fact]
    public async Task TestGetEventIdsByUserIds()
    {
        // Arrange

        string[] userIds = ["id1", "id2", "id3"];

        var events = new List<Event>
        {
            new Event
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1"
            },
            new Event
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 6),
                Title = "Excercise",
                Location = "Home",
                UserId = "id2"
            },
            new Event
            {
                EventId = 3,
                From = new TimeOnly(9, 0),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 8),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id2"
            },
            new Event
            {
                EventId = 4,
                From = new TimeOnly(11, 0),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 7),
                Title = "Shop for groceries",
                Location = "The mall",
                UserId = "id3"
            }
        };

        int[] eventIds = [1, 2, 3, 4];


        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getEventsByUserIds(userIds))
            .ReturnsAsync((events, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.getEventIdsByUserIds(userIds);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var eventIdsResult = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value).ToArray();
        Assert.Equal(4, eventIdsResult.Count());
        Assert.Equal(eventIds, eventIdsResult);
    }

    // Tests creating singular Event
    [Fact]
    public async Task TestCreateEvent()
    {
        // Arrange

        var eventDTO = new EventDTO
        {
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2026, 1, 6),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var eventt = new Event
        {
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2026, 1, 6),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.createEvent(
                It.Is<Event>(e => 
                    e.From == eventt.From &&
                    e.To == eventt.To &&
                    e.Date == eventt.Date &&
                    e.Title == eventt.Title &&
                    e.Location == eventt.Location &&
                    e.UserId == eventt.UserId
                )
            )).ReturnsAsync(OperationStatus.Ok);
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.createEvent(eventDTO);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }
    
    // Test for when new Event overlaps with already existing Events
    [Fact]
    public async Task TestValidateForCreateNotAcceptable()
    {
        // Arrange
        string[] userIds = ["id1", "id2"];

        var eventDTO = new EventDTO
        {
            From = new TimeOnly(10, 0),
            To = new TimeOnly(12, 30),
            Date = new DateOnly(2026, 1, 6),
            Title = "Shop for groceries",
            Location = "The mall",
            UserId = "id1"
        };

        var events = new List<Event>
        {   
            new Event
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1"
            },
            new Event
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 6),
                Title = "Excercise",
                Location = "Home",
                UserId = "id2"
            },
            new Event
            {
                EventId = 3,
                From = new TimeOnly(12, 30),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id2"
            }
        };

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getDatesEvents(userIds, eventDTO.Date))
            .ReturnsAsync((events, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.validateEventForCreate(eventDTO, userIds);
    
        // Assert
        var notAcceptableResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(406, notAcceptableResult.StatusCode);
    }

    // Test for when Event is valid for update
    [Fact]
    public async Task TestValidateForUpdate()
    {
        // Arrange
        string[] userIds = ["id1", "id2"];

        var eventDTO = new EventDTO
        {
            EventId = 1,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 30),
            Date = new DateOnly(2026, 1, 6),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var events = new List<Event>
        {   
            new Event
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1"
            },
            new Event
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 6),
                Title = "Excercise",
                Location = "Home",
                UserId = "id2"
            },
            new Event
            {
                EventId = 3,
                From = new TimeOnly(12, 30),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id2"
            }
        };

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getDatesEvents(userIds, eventDTO.Date))
            .ReturnsAsync((events, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.validateEventForUpdate(eventDTO, userIds);
    
        // Assert
        var notAcceptableResult = Assert.IsType<OkObjectResult>(result);
    }

    // Test for Error occuring while updating Event
    [Fact]
    public async Task TestUpdateEventNotOk()
    {
        // Arrange

        var eventDTO = new EventDTO
        {
            EventId = 1,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2026, 1, 6),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var eventt = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2026, 1, 6),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.updateEvent(
                It.Is<Event>(e => 
                    e.EventId == eventt.EventId &&
                    e.From == eventt.From &&
                    e.To == eventt.To &&
                    e.Date == eventt.Date &&
                    e.Title == eventt.Title &&
                    e.Location == eventt.Location &&
                    e.UserId == eventt.UserId
                )
            )).ReturnsAsync(OperationStatus.Error);
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.updateEvent(eventDTO);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    // Test for deleting Event
    [Fact]
    public async Task TestDeleteEvent()
    {
        // Arrange

        var eventId = 1;

        var eventt = new Event
        {
            EventId = 1,
            From = new TimeOnly(10, 30),
            To = new TimeOnly(12, 0),
            Date = new DateOnly(2026, 1, 6),
            Title = "Take medication",
            Location = "Home",
            UserId = "id1"
        };

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getEventById(eventId))
            .ReturnsAsync((eventt, OperationStatus.Ok));
        mockEventRepo
            .Setup(repo => repo.deleteEvent(eventt))
            .ReturnsAsync(OperationStatus.Ok);
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.deleteEvent(eventId);
    
        // Assert
        var errorResult = Assert.IsType<OkObjectResult>(result);
    }

    // Test for Error occuring while deleting several Events
    [Fact]
    public async Task TestDeleteEventsByIdsNotOk()
    {
        // Arrange

        int[] eventIds = [1, 2, 3];

        var events = new List<Event>
        {   
            new Event
            {
                EventId = 1,
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2026, 1, 6),
                Title = "Take medication",
                Location = "Home",
                UserId = "id1"
            },
            new Event
            {
                EventId = 2,
                From = new TimeOnly(9, 30),
                To = new TimeOnly(10, 30),
                Date = new DateOnly(2026, 1, 7),
                Title = "Excercise",
                Location = "Home",
                UserId = "id1"
            },
            new Event
            {
                EventId = 3,
                From = new TimeOnly(12, 30),
                To = new TimeOnly(13, 0),
                Date = new DateOnly(2026, 1, 8),
                Title = "Go for a walk",
                Location = "The park",
                UserId = "id1"
            }
        };

        var mockEventRepo = new Mock<IEventRepo>();
        mockEventRepo
            .Setup(repo => repo.getEventsByIds(eventIds))
            .ReturnsAsync((events, OperationStatus.Ok));
        mockEventRepo
            .Setup(repo => repo.deleteEvents(events))
            .ReturnsAsync(OperationStatus.Error);
        var mockLogger = new Mock<ILogger<EventController>>();
        var eventController = new EventController(mockEventRepo.Object, mockLogger.Object);

        // Act
        var result = await eventController.deleteEventsByIds(eventIds);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }
}