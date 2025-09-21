using Application.Events;
using Application.Contracts;
using Application.Resources;
using Application.Services.Messaging;
using AutoMapper;
using Common.Exceptions;
using Domain.Entities;
using Domain.Interfaces;
using DTO.DTO.User;
using Presentation.Utilities;
using System.IdentityModel.Tokens.Jwt;
using Application.Events;

namespace Application.Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly IJWTUtilities _jwt;
        private readonly IJwtBlacklistServices _jwtBlacklistServices;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserStatusChangeRepository _userStatusChangeRepository;
        private readonly IEventPublisher _eventPublisher;
        public UserServices(IUserRepository userRepository, IRoleRepository roleRepository, IMapper mapper, IJWTUtilities jwt, IJwtBlacklistServices jwtBlacklistServices, IAuditLogRepository auditLogRepository,
    IUserStatusChangeRepository userStatusChangeRepository,
    IEventPublisher eventPublisher)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
            _jwt = jwt;
            _jwtBlacklistServices = jwtBlacklistServices;
            _auditLogRepository = auditLogRepository;
            _userStatusChangeRepository = userStatusChangeRepository;
            _eventPublisher = eventPublisher;

        }

        public async Task<string> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, request.Password);

            if (user == null || user.Password != request.Password)
                throw new UnauthorizedException(StringResourceMessage.InvalidCredentials);

            if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                var message = string.Equals(user.Status, "Suspended", StringComparison.OrdinalIgnoreCase)
                    ? "Account is suspended."
                    : "Account is deactivated.";
                throw new UnauthorizedException(message);
            }
            var role = await _roleRepository.GetAsyncById(user.RoleId);
            var token = _jwt.GenerateToken(user.Id, user.Email, role?.Name ?? "User");

            return token;
        }


        public async Task LogoutAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new BadRequestException("Access token is missing.");

            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var jwtUser = _jwt.GetUserFromJWTToken(jwtToken);
            var userId = jwtUser.Id;
            var user = await _userRepository.GetAsyncById(userId);
            if (user == null)
                throw new NotFoundException(StringResourceMessage.UserNotFound);

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _userRepository.UpdateAsync(user);
            await _jwtBlacklistServices.AddToBlacklistAsync(token);
        }
        public async Task LogoutAsync(string token, Guid userId, string? jti, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                await _jwtBlacklistServices.AddToBlacklistAsync(token);

            await _eventPublisher.PublishAsync(
                "UserLoggedOut",
                new UserLoggedOutEvent(userId, DateTime.UtcNow, jti),
                ct
            );
        }

        public async Task<RegisterResponseDto> RegisterUserAsync(RegisterRequestDto request)
        {
            if (await _userRepository.FindByEmailOrUsernameAsync(request.Email, request.Username) != null)
                throw new ConflictException(StringResourceMessage.UserAlreadyExists);
            var role = await _roleRepository.GetRoleByName(request.Role);
            if (role == null)
                throw new NotFoundException(StringResourceMessage.RoleNotFound);
            var oid = await _userRepository.AddAsync(_mapper.Map<User>(request, opt => opt.AfterMap((o, dest) =>
            {
                dest.RoleId = role.Id;
                dest.Status = Status.Active.ToString();
            })));
            return new RegisterResponseDto() { Id = oid };
        }
        public async Task ChangeUserStatusAsync(Guid targetUserId, Guid adminUserId, AdminChangeUserStatusRequestDto request)
        {
            var user = await _userRepository.GetAsyncById(targetUserId)
                       ?? throw new NotFoundException("User not found.");

            var newStatus = request.Status?.Trim();
            if (string.IsNullOrEmpty(newStatus))
                throw new BadRequestException("Status is required.");

            var allowed = new[] { "Active", "Suspended", "Deactivated" };
            if (!allowed.Contains(newStatus))
                throw new BadRequestException($"Status must be one of: {string.Join(", ", allowed)}");

            if (string.Equals(user.Status, newStatus, StringComparison.OrdinalIgnoreCase))
                return;

            var oldStatus = user.Status;

            user.Status = newStatus;
            await _userRepository.UpdateAsync(user);

            await _auditLogRepository.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                TargetId = user.Id,
                ActionType = "UserStatusChanged",
                Timestamp = DateTime.UtcNow,
                Description = $"Admin {adminUserId} changed user {user.Id} status from {oldStatus} to {newStatus}."
            });

            await _userStatusChangeRepository.AddAsync(new UserStatusChange
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ChangedByAdminId = adminUserId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow
            });

            await _eventPublisher.PublishAsync(
             "user.status.changed",
                 new UserStatusChangedEvent
                 {
                     UserId = user.Id,
                     OldStatus = oldStatus,
                     NewStatus = newStatus,
                     ChangedBy = adminUserId,
                     ChangedAtUtc = DateTime.UtcNow
                 });

        }


    }
}

