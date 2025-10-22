using FormsBackend.Controllers;
using FormsBackend.DTOs;
using FormsBackend.Services;
using FormsBackend.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace FormsBackend.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly Mock<IResponseCookies> _mockResponseCookies;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserRepository = new Mock<IUserRepository>();
            
            // Setup HttpContext mocks
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockResponseCookies = new Mock<IResponseCookies>();
            
            // Setup the mock chain
            _mockHttpResponse.Setup(r => r.Cookies).Returns(_mockResponseCookies.Object);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);
            
            // Setup default user (for tests that don't need authentication)
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _mockHttpContext.Setup(c => c.User).Returns(principal);

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockConfiguration.Object,
                _mockUserRepository.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        #region Register Tests

        [Fact]
        public async Task Register_WithValidDto_ReturnsOkWithSuccessStatus()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test@123"
            };

            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = "Learner"
            };

            _mockAuthService
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ReturnsAsync((userResponse, "test-jwt-token"));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Registration successful");
            response.User.Should().NotBeNull();
            response.User.Id.Should().Be(1);
            response.User.Username.Should().Be("testuser");
            response.User.Email.Should().Be("test@example.com");
            response.User.Role.Should().Be("Learner");

            // Verify cookie was set
            _mockResponseCookies.Verify(c => c.Append(
                "auth-token", 
                "test-jwt-token", 
                It.IsAny<CookieOptions>()), 
                Times.Once);
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "existing@example.com",
                Password = "Test@123"
            };

            _mockAuthService
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ThrowsAsync(new InvalidOperationException("Email already registered"));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Email already registered");
            response.User.Should().BeNull();

            // Verify no cookie was set
            _mockResponseCookies.Verify(c => c.Append(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CookieOptions>()), 
                Times.Never);
        }

        [Fact]
        public async Task Register_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test@123"
            };

            _mockAuthService
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
            
            var response = statusCodeResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            response.Success.Should().BeFalse();
            response.Message.Should().Be("An error occurred during registration");
            response.User.Should().BeNull();
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithSuccessStatus()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Test@123"
            };

            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = "Admin"
            };

            _mockAuthService
                .Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync((userResponse, "test-jwt-token"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Login successful");
            response.User.Should().NotBeNull();
            response.User.Email.Should().Be("test@example.com");

            // Verify cookie was set
            _mockResponseCookies.Verify(c => c.Append(
                "auth-token", 
                "test-jwt-token", 
                It.IsAny<CookieOptions>()), 
                Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _mockAuthService
                .Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new InvalidOperationException("Invalid email or password"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var response = unauthorizedResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Invalid email or password");
            response.User.Should().BeNull();
        }

        [Fact]
        public async Task Login_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Test@123"
            };

            _mockAuthService
                .Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
            
            var response = statusCodeResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            response.Success.Should().BeFalse();
            response.Message.Should().Be("An error occurred during login");
            response.User.Should().BeNull();
        }

        #endregion

        #region Logout Tests

        [Fact]
        public void Logout_WhenCalled_ReturnsOkAndClearsAuthCookie()
        {
            // Arrange
            SetupAuthenticatedUser();

            // Act
            var result = _controller.Logout();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Logged out successfully");
            response.User.Should().BeNull();

            // Verify cookie deletion
            _mockResponseCookies.Verify(c => c.Delete(
                "auth-token", 
                It.Is<CookieOptions>(opts => 
                    opts.HttpOnly == true && 
                    opts.Secure == true && 
                    opts.SameSite == SameSiteMode.Strict && 
                    opts.Path == "/")), 
                Times.Once);
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public async Task GetCurrentUser_WithValidUser_ReturnsOkWithUserData()
        {
            // Arrange
            SetupAuthenticatedUser(userId: "123");

            var userResponse = new UserResponseDto
            {
                Id = 123,
                Username = "testuser",
                Email = "test@example.com",
                Role = "Learner"
            };

            _mockAuthService
                .Setup(x => x.GetCurrentUserAsync(123))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            
            response.Success.Should().BeTrue();
            response.Message.Should().Be("User retrieved successfully");
            response.User.Should().NotBeNull();
            response.User.Id.Should().Be(123);
        }

        [Fact]
        public async Task GetCurrentUser_WithNoUserIdClaim_ReturnsOkWithDefaultUserId()
        {
            // Arrange
            SetupAuthenticatedUser(includeNameIdentifier: false);

            var userResponse = new UserResponseDto
            {
                Id = 0,
                Username = "defaultuser",
                Email = "default@example.com",
                Role = "Learner"
            };

            _mockAuthService
                .Setup(x => x.GetCurrentUserAsync(0))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            
            response.Success.Should().BeTrue();
            response.User.Should().NotBeNull();
            response.User.Id.Should().Be(0);
        }

        
        [Fact]
        public async Task GetCurrentUser_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            SetupAuthenticatedUser(userId: "123");

            _mockAuthService
                .Setup(x => x.GetCurrentUserAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("User not found"));

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
            
            var response = statusCodeResult.Value.Should().BeAssignableTo<AuthStatusDto>().Subject;
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Error retrieving user information");
            response.User.Should().BeNull();
        }

        #endregion

        #region Helper Methods

        private void SetupAuthenticatedUser(string userId = "1", bool includeNameIdentifier = true)
        {
            var claims = new List<Claim>();
            
            if (includeNameIdentifier)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }
            
            claims.Add(new Claim(ClaimTypes.Name, "testuser"));
            claims.Add(new Claim(ClaimTypes.Email, "test@example.com"));
            claims.Add(new Claim(ClaimTypes.Role, "Learner"));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            
            _mockHttpContext.Setup(c => c.User).Returns(principal);
        }

        #endregion
    }
}
