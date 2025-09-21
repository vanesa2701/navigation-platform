using Application.Contracts;
using Application.Resources;
using Application.Services;
using Application.Services.Messaging;
using Application.Utilities;
using AutoMapper;
using Common.Exceptions;
using Domain.Entities;
using Domain.Interfaces;
using DTO.DTO.User;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Presentation.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.UserTests
{
    public class UserServicesTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IJWTUtilities> _jwtMock;
        private readonly Mock<IJwtBlacklistServices> _jwtBlacklistServicesMock;
        private readonly Mock<IAuditLogRepository> _auditLogRepository;
        private readonly Mock<IUserStatusChangeRepository> _userStatusChangeRepository;
        private readonly Mock<IEventPublisher> _eventPublisher;
        private readonly UserServices _userServices;

        public UserServicesTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _mapperMock = new Mock<IMapper>();
            _jwtMock = new Mock<IJWTUtilities>();
            _jwtBlacklistServicesMock = new Mock<IJwtBlacklistServices>();
            _auditLogRepository = new Mock<IAuditLogRepository>();
            _userStatusChangeRepository = new Mock<IUserStatusChangeRepository>();
            _eventPublisher = new Mock<IEventPublisher>();

            _userServices = new UserServices(
                _userRepositoryMock.Object,
                _roleRepositoryMock.Object,
                _mapperMock.Object,
                _jwtMock.Object,
                _jwtBlacklistServicesMock.Object,
                _auditLogRepository.Object,
                _userStatusChangeRepository.Object,
                _eventPublisher.Object
            );
        }

        #region Login

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "password123" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Password = request.Password,
                RoleId = Guid.NewGuid(),
                Status = "Active"
            };
            var role = new Role { Id = user.RoleId, Name = "Admin" };
            var expectedToken = "fake-jwt-token";

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, request.Password)).ReturnsAsync(user);
            _roleRepositoryMock.Setup(r => r.GetAsyncById(user.RoleId)).ReturnsAsync(role);
            _jwtMock.Setup(j => j.GenerateToken(user.Id, user.Email, role.Name)).Returns(expectedToken);

            // Act
            var result = await _userServices.LoginAsync(request);

            // Assert
            Assert.Equal(expectedToken, result);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorized_WhenUserNotFound()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "notfound@example.com", Password = "password" };
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, request.Password))
                               .ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _userServices.LoginAsync(request));
            Assert.Equal(StringResourceMessage.InvalidCredentials, ex.Message);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordMismatch()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "wrong" };
            var user = new User { Id = Guid.NewGuid(), Email = request.Email, Password = "correct", RoleId = Guid.NewGuid() };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, request.Password))
                               .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _userServices.LoginAsync(request));
            Assert.Equal(StringResourceMessage.InvalidCredentials, ex.Message);
        }

        #endregion

        #region Register

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnResponse_WhenSuccessful()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                Email = "newuser@example.com",
                Username = "newuser",
                Password = "123",
                Role = "User"
            };

            var role = new Role { Id = Guid.NewGuid(), Name = "User" };
            var mappedUser = new User
            {
                Email = request.Email,
                Username = request.Username,
                Password = request.Password,
                RoleId = role.Id,
                Status = Status.Active.ToString(),
                Id = Guid.NewGuid()
            };

            _userRepositoryMock.Setup(r => r.FindByEmailOrUsernameAsync(request.Email, request.Username))
                               .ReturnsAsync((User)null);
            _roleRepositoryMock.Setup(r => r.GetRoleByName(request.Role)).ReturnsAsync(role);
            _mapperMock.Setup(m => m.Map<User>(It.IsAny<RegisterRequestDto>(), It.IsAny<Action<IMappingOperationOptions>>()))
                       .Returns(mappedUser);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(mappedUser.Id);

            // Act
            var result = await _userServices.RegisterUserAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mappedUser.Id, result.Id);
            Assert.Equal(role.Id, mappedUser.RoleId);
            Assert.Equal(Status.Active.ToString(), mappedUser.Status);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrowConflict_WhenUserExists()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                Email = "existing@example.com",
                Username = "existinguser",
                Role = "User"
            };

            _userRepositoryMock.Setup(r => r.FindByEmailOrUsernameAsync(request.Email, request.Username))
                               .ReturnsAsync(new User());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConflictException>(() => _userServices.RegisterUserAsync(request));
            Assert.Equal(StringResourceMessage.UserAlreadyExists, ex.Message);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrowNotFound_WhenRoleMissing()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                Email = "new@example.com",
                Username = "newuser",
                Role = "InvalidRole"
            };

            _userRepositoryMock.Setup(r => r.FindByEmailOrUsernameAsync(request.Email, request.Username))
                               .ReturnsAsync((User)null);
            _roleRepositoryMock.Setup(r => r.GetRoleByName(request.Role))
                               .ReturnsAsync((Role)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _userServices.RegisterUserAsync(request));
            Assert.Equal(StringResourceMessage.RoleNotFound, ex.Message);
        }

        #endregion

        #region Logout

        [Fact]
        public async Task LogoutAsync_ShouldBlacklistToken_AndClearRefresh()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // 256-bit key for HS256
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = new JwtSecurityToken(
                claims: new[] { new Claim("sub", userId.ToString()) },
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds
            );
            var token = tokenHandler.WriteToken(jwt);

            var jwtUser = new JwtUser { Id = userId };

            _jwtMock.Setup(j => j.GetUserFromJWTToken(It.IsAny<JwtSecurityToken>()))
                    .Returns(jwtUser);

            _userRepositoryMock.Setup(r => r.GetAsyncById(userId))
                               .ReturnsAsync(new User { Id = userId, Status = "Active" });

            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                               .Returns(Task.CompletedTask);

            _jwtBlacklistServicesMock.Setup(s => s.AddToBlacklistAsync(token))
                                     .Returns(Task.CompletedTask)
                                     .Verifiable();

            // Act
            await _userServices.LogoutAsync(token);

            // Assert
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Id == userId && u.RefreshToken == null)), Times.Once);
            _jwtBlacklistServicesMock.Verify(s => s.AddToBlacklistAsync(token), Times.Once);
        }

        #endregion
    }
}
