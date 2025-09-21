using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IJourneyRepository : IRepositoryBase<Journey>
    {
        Task<Journey> GetJourneyByUserIdAndStartTime(Guid userId, DateTime dateTime);
        Task<List<Journey>> GetJourneysByUserIdAsync(Guid userId);
        IQueryable<Journey> GetAllJournies();
        Task<IReadOnlyList<Journey>> GetJourneysForDateAsync(DateTime date, CancellationToken ct = default); // +ct
        Task UpdateAsync(Journey journey, CancellationToken ct = default);
    }
}

