namespace Application.Contracts
{
    public interface IJwtBlacklistServices
    {
        Task AddToBlacklistAsync(string token);
        Task<bool> IsBlacklistedAsync(string token);
    }

}
