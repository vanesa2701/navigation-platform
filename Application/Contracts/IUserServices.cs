using DTO.DTO.User;

namespace Application.Contracts
{
    public interface IUserServices
    {
        Task<RegisterResponseDto> RegisterUserAsync(RegisterRequestDto request);
        Task<string> LoginAsync(LoginRequestDto request);
        public Task LogoutAsync(string token);
        Task ChangeUserStatusAsync(Guid targetUserId, Guid adminUserId, DTO.DTO.User.AdminChangeUserStatusRequestDto request);
        Task LogoutAsync(string token, Guid userId, string? jti, CancellationToken ct = default);
    }
}

