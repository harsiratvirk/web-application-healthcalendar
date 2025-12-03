using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.AspNetCore.Identity;
using HealthCalendar.DAL;
using HealthCalendar.Controllers;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using HealthCalendar.DTOs;

public class AuthControllerTests
{
    // Test for registering Patient
    [Fact]
    public async Task TestRegisterPatient()
    {
        // Arrange

        var registerDTO = new RegisterDTO {
            Name = "Bob",
            Email = "bob@gmail.com",
            Password = "Password"
        };

        var patient = new User
        {
            Name = "Bob",
            UserName = "bob@gmail.com",
            Role = Roles.Patient
        };

        var mockAuthRepo = new Mock<IAuthRepo>();
        mockAuthRepo
            .Setup(repo => repo.registerUser(It.Is<User>(u =>
                    u.UserName == patient.UserName &&
                    u.Name == patient.Name &&
                    u.Role == patient.Role 
                ), 
                registerDTO.Password
                )
            ).ReturnsAsync((OperationStatus.Ok, []));
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var authController = new AuthController(mockAuthRepo.Object, mockConfiguration.Object, 
                                                mockLogger.Object);

        // Act
        var result = await authController.registerPatient(registerDTO);
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }
    
    // Test for unauthorized User login
    [Fact]
    public async Task TestLoginUnauthorized()
    {
        // Arrange

        var loginDTO = new LoginDTO {
            Email = "bob@gmail.com",
            Password = "worngPassword"
        };

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

        var mockAuthRepo = new Mock<IAuthRepo>();
        mockAuthRepo
            .Setup(repo => repo.getUserByUsername(loginDTO.Email))
            .ReturnsAsync((user, OperationStatus.Ok));
        mockAuthRepo
            .Setup(repo => repo.checkPassword(user, loginDTO.Password))
            .ReturnsAsync(OperationStatus.Unauthorized);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var authController = new AuthController(mockAuthRepo.Object, mockConfiguration.Object, 
                                                mockLogger.Object);

        // Act
        var result = await authController.login(loginDTO);
    
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // Test for User logout
    [Fact]
    public async Task TestLogout()
    {
        // Arrange

        var mockAuthRepo = new Mock<IAuthRepo>();
        mockAuthRepo
            .Setup(repo => repo.logout())
            .ReturnsAsync(OperationStatus.Ok);
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var authController = new AuthController(mockAuthRepo.Object, mockConfiguration.Object, 
                                                mockLogger.Object);

        // Act
        var result = await authController.logout();
    
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

}