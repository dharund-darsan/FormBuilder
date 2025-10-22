using System;
using System.Threading.Tasks;
using FormsBackend.DTOs;
using FormsBackend.Models.Sql;
using FormsBackend.Repositories;
using FormsBackend.Services;
using FormsBackend.Enums;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace FormsBackend.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup configuration for JWT
            _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns("aVeryLongSuperSecretKeyThatIsAtLeast32Chars!");
            _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("FormsApp");
            _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("FormsAppUsers");
            
            _authService = new AuthService(_mockUserRepository.Object, _mockConfiguration.Object);
        }

        #region RegisterAsync Tests

        // [Fact]
        // public async Task RegisterAsync_NewUser_Success_ReturnsUserAndToken()
        // {
        //     // Arrange
        //     var registerDto = new RegisterDto
        //     {
        //         Username = "testuser",
        //         Email = "test@example.com",
        //         Password = "Password123!"
        //     };
        //
        //     var createdUser = new User
        //     {
        //         Id = 1,
        //         Username = "testuser",
        //         Email = "test@example.com",
        //         Role = UserRole.Learner,
        //         PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
        //         CreatedAt = DateTime.UtcNow
        //     };
        //
        //     _mockUserRepository.Setup(x => x.GetByEmailAsync(registerDto.Email))
        //         .ReturnsAsync((User)null);
        //
        //     _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>()))
        //         .ReturnsAsync(createdUser);
        //
        //     // Act
        //     var result = await _authService.RegisterAsync(registerDto);
        //
        //     // Assert
        //     Assert.NotNull(result.User);
        //     Assert.Equal(1, result.User.Id);
        //     Assert.Equal("testuser", result.User.Username);
        //     Assert.Equal("test@example.com", result.User.Email);
        //     Assert.Equal("Learner", result.User.Role);
        //     Assert.NotNull(result.Token);
        //     Assert.NotEmpty(result.Token);
        //
        //     _mockUserRepository.Verify(x => x.GetByEmailAsync(registerDto.Email), Times.Once);
        //     _mockUserRepository.Verify(x => x.CreateAsync(It.Is<User>(u => 
        //         u.Username == registerDto.Username && 
        //         u.Email == registerDto.Email && 
        //         u.Role == UserRole.Learner &&
        //         BCrypt.Net.BCrypt.Verify(registerDto.Password, u.PasswordHash)
        //     )), Times.Once);
        // }

        [Fact]
        public async Task RegisterAsync_ExistingEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "existing@example.com",
                Password = "Password123!"
            };

            var existingUser = new User
            {
                Id = 1,
                Username = "existinguser",
                Email = "existing@example.com"
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(registerDto.Email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.RegisterAsync(registerDto));
            
            Assert.Equal("Email already registered", exception.Message);
            
            _mockUserRepository.Verify(x => x.GetByEmailAsync(registerDto.Email), Times.Once);
            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_VerifyPasswordHashing()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "MySecurePassword123!"
            };

            User capturedUser = null;

            _mockUserRepository.Setup(x => x.GetByEmailAsync(registerDto.Email))
                .ReturnsAsync((User)null);

            _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => new User
                {
                    Id = 1,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    PasswordHash = u.PasswordHash
                });

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(capturedUser);
            Assert.True(BCrypt.Net.BCrypt.Verify(registerDto.Password, capturedUser.PasswordHash));
            Assert.Equal(UserRole.Learner, capturedUser.Role);
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_ValidCredentials_Success_ReturnsUserAndToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!");
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = UserRole.Admin,
                PasswordHash = hashedPassword
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result.User);
            Assert.Equal(1, result.User.Id);
            Assert.Equal("testuser", result.User.Username);
            Assert.Equal("test@example.com", result.User.Email);
            Assert.Equal("Admin", result.User.Role);
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);

            _mockUserRepository.Verify(x => x.GetByEmailAsync(loginDto.Email), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync((User)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(loginDto));
            
            Assert.Equal("Invalid email or password", exception.Message);
            
            _mockUserRepository.Verify(x => x.GetByEmailAsync(loginDto.Email), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsInvalidOperationException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword!"
            };

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!");
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = hashedPassword
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(loginDto));
            
            Assert.Equal("Invalid email or password", exception.Message);
            
            _mockUserRepository.Verify(x => x.GetByEmailAsync(loginDto.Email), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_LearnerRole_ReturnsCorrectRole()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "learner@example.com",
                Password = "Password123!"
            };

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!");
            var user = new User
            {
                Id = 2,
                Username = "learner",
                Email = "learner@example.com",
                Role = UserRole.Learner,
                PasswordHash = hashedPassword
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Equal("Learner", result.User.Role);
        }

        #endregion

        #region GetCurrentUserAsync Tests

        [Fact]
        public async Task GetCurrentUserAsync_ExistingUser_ReturnsUserResponseDto()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Role = UserRole.Admin
            };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.GetCurrentUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("Admin", result.Role);
            
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var userId = 999;
            
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.GetCurrentUserAsync(userId));
            
            Assert.Equal("User not found", exception.Message);
            
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserAsync_LearnerUser_ReturnsCorrectRole()
        {
            // Arrange
            var userId = 2;
            var user = new User
            {
                Id = userId,
                Username = "learner",
                Email = "learner@example.com",
                Role = UserRole.Learner
            };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.GetCurrentUserAsync(userId);

            // Assert
            Assert.Equal("Learner", result.Role);
        }

        #endregion

        #region GenerateJwtToken Tests

        [Fact]
        public void GenerateJwtToken_ValidUser_ReturnsValidToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = UserRole.Admin
            };

            // Act
            var token = _authService.GenerateJwtToken(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Validate token structure
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);
            
            Assert.NotNull(jwt);
            Assert.Equal("FormsApp", jwt.Issuer);
            Assert.Contains("FormsAppUsers", jwt.Audiences);
            
            // Validate claims
            var claims = jwt.Claims.ToList();
            Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1");
            Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
            Assert.Contains(claims, c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
            Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
            
            // Validate expiry (should be 7 days from now)
            Assert.True(jwt.ValidTo > DateTime.UtcNow.AddDays(6));
            Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddDays(8));
        }

        [Fact]
        public void GenerateJwtToken_LearnerUser_ContainsCorrectRole()
        {
            // Arrange
            var user = new User
            {
                Id = 2,
                Username = "learner",
                Email = "learner@example.com",
                Role = UserRole.Learner
            };

            // Act
            var token = _authService.GenerateJwtToken(user);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);
            
            var claims = jwt.Claims.ToList();
            Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Learner");
        }

        [Fact]
        public void GenerateJwtToken_DifferentUsers_GeneratesDifferentTokens()
        {
            // Arrange
            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Role = UserRole.Admin
            };

            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Role = UserRole.Learner
            };

            // Act
            var token1 = _authService.GenerateJwtToken(user1);
            var token2 = _authService.GenerateJwtToken(user2);

            // Assert
            Assert.NotEqual(token1, token2);
        }

        #endregion
    }
}
