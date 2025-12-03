using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using HealthCalendar.DAL;
using HealthCalendar.Controllers;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using HealthCalendar.DTOs;

public class UserControllerTests
{
    
    // Test for Error occuring when retreiving User
    [Fact]
    public async Task TestGetUserNotOk()
    {
        // Arrange

        var userId = "id1";

        var mockUserRepo = new Mock<IUserRepo>();
        mockUserRepo
            .Setup(repo => repo.getUserById(userId))
            .ReturnsAsync((null, OperationStatus.Error));
        var mockLogger = new Mock<ILogger<UserController>>();
        var userController = new UserController(mockUserRepo.Object, mockLogger.Object);

        // Act
        var result = await userController.getUser(userId);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }
    
    // Test for Error occuring when retreiving list of Users
    [Fact]
    public async Task TestGetUsersByWorkerIdNotOk()
    {
        // Arrange

        var workerId = "id1";

        var mockUserRepo = new Mock<IUserRepo>();
        mockUserRepo
            .Setup(repo => repo.getUsersByWorkerId(workerId))
            .ReturnsAsync(([], OperationStatus.Error));
        var mockLogger = new Mock<ILogger<UserController>>();
        var userController = new UserController(mockUserRepo.Object, mockLogger.Object);

        // Act
        var result = await userController.getUsersByWorkerId(workerId);
    
        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }

    // Test for Retreiving list of Patients
    [Fact]
    public async Task TestGetUnassignedPatients()
    {
        // Arrange
        var patients = new List<User>
        {
            new User
            {
                Id = "id1",
                UserName = "lars@gmail.com",
                PasswordHash = "secret",
                Name = "Lars",
                Role = Roles.Patient,
                WorkerId = null,
                Worker = null
            },
            new User
            {
                Id = "id2",
                UserName = "karl@gmail.com",
                PasswordHash = "secret",
                Name = "Karl",
                Role = Roles.Patient,
                WorkerId = null,
                Worker = null
            }
        };

        var patientDTOs = new List<UserDTO>
        {
            new UserDTO
            {
                Id = "id1",
                UserName = "lars@gmail.com",
                Name = "Lars",
                Role = Roles.Patient,
                WorkerId = null,
            },
            new UserDTO
            {
                Id = "id2",
                UserName = "karl@gmail.com",
                Name = "Karl",
                Role = Roles.Patient,
                WorkerId = null
            }
        };

        var mockUserRepo = new Mock<IUserRepo>();
        mockUserRepo
            .Setup(repo => repo.getUnassignedPatients())
            .ReturnsAsync((patients, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<UserController>>();
        var userController = new UserController(mockUserRepo.Object, mockLogger.Object);

        // Act
        var result = await userController.getUnassignedPatients();
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var patientDTOsResult = Assert.IsAssignableFrom<IEnumerable<UserDTO>>(okResult.Value).ToList();
        Assert.Equal(2, patientDTOsResult.Count());
        Assert.Equal(
            patientDTOs.Select(u => (u.Id, u.UserName, u.Name, u.Role, u.WorkerId)), 
            patientDTOsResult.Select(u => (u.Id, u.UserName, u.Name, u.Role, u.WorkerId))
        );
    }
    
    // Test for Retreiving Ids from Patients related to Worker
    [Fact]
    public async Task TestGetIdsByWorkerId()
    {
        // Arrange
        var workedId = "id1";

        var worker = new User
        {
            Id = "id1",
            UserName = "bob@gmail.com",
            PasswordHash = "secret",
            Name = "Bob",
            Role = Roles.Worker,
            WorkerId = null,
            Worker = null
        };

        var patients = new List<User>
        {
            new User
            {
                Id = "id2",
                UserName = "lars@gmail.com",
                PasswordHash = "secret",
                Name = "Lars",
                Role = Roles.Patient,
                WorkerId = "id1",
                Worker = worker
            },
            new User
            {
                Id = "id3",
                UserName = "karl@gmail.com",
                PasswordHash = "secret",
                Name = "Karl",
                Role = Roles.Patient,
                WorkerId = "id1",
                Worker = worker
            }
        };

        string[] patientIds = ["id2", "id3"];

        var mockUserRepo = new Mock<IUserRepo>();
        mockUserRepo
            .Setup(repo => repo.getUsersByWorkerId(workedId))
            .ReturnsAsync((patients, OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<UserController>>();
        var userController = new UserController(mockUserRepo.Object, mockLogger.Object);

        // Act
        var result = await userController.getIdsByWorkerId(workedId);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var patientIdsResult = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value).ToArray();
        Assert.Equal(2, patientIdsResult.Count());
        Assert.Equal(patientIds, patientIdsResult);
    }

    // Test For Patients not being found when trying to unassign them from Worker
    [Fact]
    public async Task TestUnassignPatientNotFound()
    {
        // Arrange

        string[] patientIds = ["id1", "id2"];

        var mockUserRepo = new Mock<IUserRepo>();
        mockUserRepo
            .Setup(repo => repo.getUsersByIds(patientIds))
            .ReturnsAsync(([], OperationStatus.Ok));
        var mockLogger = new Mock<ILogger<UserController>>();
        var userController = new UserController(mockUserRepo.Object, mockLogger.Object);

        // Act
        var result = await userController.unassignPatientsFromWorker(patientIds);
    
        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
    }
    // Test for Assigning list of Patients to Worker
    [Fact]
    public async Task TestAssignPatientsToWorker()
    {
        // Arrange
        var workedId = "id1";
        string[] patientIds = ["id2", "id3"];

        var worker = new User
        {
            Id = "id1",
            UserName = "bob@gmail.com",
            PasswordHash = "secret",
            Name = "Bob",
            Role = Roles.Worker,
            WorkerId = null,
            Worker = null
        };

        var oldPatients = new List<User>
        {
            new User
            {
                Id = "id2",
                UserName = "lars@gmail.com",
                PasswordHash = "secret",
                Name = "Lars",
                Role = Roles.Patient,
                WorkerId = null,
                Worker = null
            },
            new User
            {
                Id = "id3",
                UserName = "karl@gmail.com",
                PasswordHash = "secret",
                Name = "Karl",
                Role = Roles.Patient,
                WorkerId = null,
                Worker = null
            }
        };

        var updatedPatient1 = new User
        {
            Id = "id2",
            UserName = "lars@gmail.com",
            PasswordHash = "secret",
            Name = "Lars",
            Role = Roles.Patient,
            WorkerId = workedId,
            Worker = worker
        };
        var updatedPatient2 = new User
        {
            Id = "id3",
            UserName = "karl@gmail.com",
            PasswordHash = "secret",
            Name = "Karl",
            Role = Roles.Patient,
            WorkerId = workedId,
            Worker = worker
        };

        var mockUserRepo = new Mock<IUserRepo>();
        mockUserRepo
            .Setup(repo => repo.getUserById(workedId))
            .ReturnsAsync((worker, OperationStatus.Ok));
        mockUserRepo
            .Setup(repo => repo.getUsersByIds(patientIds))
            .ReturnsAsync((oldPatients, OperationStatus.Ok));
        mockUserRepo
        .Setup(repo => repo.updateUser(
                It.Is<User>(u =>
                    u.Id == updatedPatient1.Id &&
                    u.UserName == updatedPatient1.UserName &&
                    u.PasswordHash == updatedPatient1.PasswordHash &&
                    u.Name == updatedPatient1.Name &&
                    u.Role == updatedPatient1.Role &&
                    u.WorkerId == updatedPatient1.WorkerId &&
                    u.Worker == updatedPatient1.Worker
                )
            )).ReturnsAsync(OperationStatus.Ok);
        mockUserRepo
        .Setup(repo => repo.updateUser(
                It.Is<User>(u =>
                    u.Id == updatedPatient2.Id &&
                    u.UserName == updatedPatient2.UserName &&
                    u.PasswordHash == updatedPatient2.PasswordHash &&
                    u.Name == updatedPatient2.Name &&
                    u.Role == updatedPatient2.Role &&
                    u.WorkerId == updatedPatient2.WorkerId &&
                    u.Worker == updatedPatient2.Worker
                )
            )).ReturnsAsync(OperationStatus.Ok);
        var mockLogger = new Mock<ILogger<UserController>>();
        var userController = new UserController(mockUserRepo.Object, mockLogger.Object);

        // Act
        var result = await userController.assignPatientsToWorker(patientIds, workedId);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

    }

    // Test for deletion of User
    [Fact]
    public async Task TestDeleteUser()
    {
        // Arrange
        var userId = "id1";

        var user = new User
        {
            Id = "id1",
            UserName = "bob@gmail.com",
            PasswordHash = "secret",
            Name = "Bob",
            Role = Roles.Worker,
            WorkerId = null,
            Worker = null
        };

        var mockUserRepo = new Mock<IUserRepo>();
        mockUserRepo
            .Setup(repo => repo.getUserById(userId))
            .ReturnsAsync((user, OperationStatus.Ok));
        mockUserRepo
            .Setup(repo => repo.deleteUser(user))
            .ReturnsAsync(OperationStatus.Ok);
        var mockLogger = new Mock<ILogger<UserController>>();
        var userController = new UserController(mockUserRepo.Object, mockLogger.Object);

        // Act
        var result = await userController.deleteUser(userId);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }
}