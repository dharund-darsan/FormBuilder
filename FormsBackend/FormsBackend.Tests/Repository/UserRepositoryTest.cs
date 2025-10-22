using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using FormsBackend.Data;
using FormsBackend.Enums;
using FormsBackend.Models.Sql;
using FormsBackend.Repositories;
using Moq;
using Moq.Dapper;
using Xunit;

namespace FormsBackend.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private readonly Mock<IDbConnectionFactory> _mockConnectionFactory;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly UserRepository _userRepository;

        public UserRepositoryTests()
        {
            _mockConnectionFactory = new Mock<IDbConnectionFactory>();
            _mockConnection = new Mock<IDbConnection>();
            _mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(_mockConnection.Object);
            _userRepository = new UserRepository(_mockConnectionFactory.Object);
        }

        #region GetByEmailAsync Tests

        [Fact]
        public async Task GetByEmailAsync_UserExists_ReturnsUser()
        {
            // Arrange
            var email = "test@example.com";
            var expectedUser = new User
            {
                Id = 1,
                Username = "testuser",
                Email = email,
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow
            };

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _userRepository.GetByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Id, result.Id);
            Assert.Equal(expectedUser.Username, result.Username);
            Assert.Equal(expectedUser.Email, result.Email);
            Assert.Equal(expectedUser.PasswordHash, result.PasswordHash);
            Assert.Equal(expectedUser.Role, result.Role);
            Assert.Equal(expectedUser.CreatedAt, result.CreatedAt);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task GetByEmailAsync_UserDoesNotExist_ReturnsNull()
        {
            // Arrange
            var email = "nonexistent@example.com";

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userRepository.GetByEmailAsync(email);

            // Assert
            Assert.Null(result);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task GetByEmailAsync_EmptyEmail_ReturnsNull()
        {
            // Arrange
            var email = "";

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userRepository.GetByEmailAsync(email);

            // Assert
            Assert.Null(result);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task GetByEmailAsync_NullEmail_HandledGracefully()
        {
            // Arrange
            string email = null;

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userRepository.GetByEmailAsync(email);

            // Assert
            Assert.Null(result);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidUser_ReturnsUserWithId()
        {
            // Arrange
            var user = new User
            {
                Username = "newuser",
                Email = "newuser@example.com",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow
            };
            var expectedId = 42;

            _mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _userRepository.CreateAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedId, result.Id);
            Assert.Equal(user.Username, result.Username);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.PasswordHash, result.PasswordHash);
            Assert.Equal(user.Role, result.Role);
            Assert.Equal(user.CreatedAt, result.CreatedAt);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_AdminUser_ReturnsUserWithAdminRole()
        {
            // Arrange
            var user = new User
            {
                Username = "adminuser",
                Email = "admin@example.com",
                PasswordHash = "hashedpassword",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };
            var expectedId = 1;

            _mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _userRepository.CreateAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedId, result.Id);
            Assert.Equal(UserRole.Admin, result.Role);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_UserWithExistingId_IdIsOverwritten()
        {
            // Arrange
            var user = new User
            {
                Id = 999, // This should be overwritten
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow
            };
            var expectedId = 5;

            _mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _userRepository.CreateAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedId, result.Id);
            Assert.NotEqual(999, result.Id);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

       
        
        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_UserExists_ReturnsUser()
        {
            // Arrange
            var id = 1;
            var expectedUser = new User
            {
                Id = id,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow
            };

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _userRepository.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Id, result.Id);
            Assert.Equal(expectedUser.Username, result.Username);
            Assert.Equal(expectedUser.Email, result.Email);
            Assert.Equal(expectedUser.PasswordHash, result.PasswordHash);
            Assert.Equal(expectedUser.Role, result.Role);
            Assert.Equal(expectedUser.CreatedAt, result.CreatedAt);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
        {
            // Arrange
            var id = 999;

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userRepository.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ZeroId_ReturnsNull()
        {
            // Arrange
            var id = 0;

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userRepository.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NegativeId_ReturnsNull()
        {
            // Arrange
            var id = -1;

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userRepository.GetByIdAsync(id);

            // Assert
            Assert.Null(result);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        
        [Fact]
        public async Task GetByIdAsync_MaxIntId_HandledProperly()
        {
            // Arrange
            var id = int.MaxValue;
            var expectedUser = new User
            {
                Id = id,
                Username = "maxuser",
                Email = "max@example.com",
                PasswordHash = "hashedpassword",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            _mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _userRepository.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            
            // Verify connection was created and disposed
            _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        #endregion
    }
}
