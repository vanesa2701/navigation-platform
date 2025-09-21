using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IJourneyShareRepository : IRepositoryBase<JourneyShare>
    {
        IQueryable<JourneyShare> Query();
        Task DeleteAll(List<JourneyShare> journeyShares);
        Task RemoveAsync(JourneyShare journeyShare);
        Task<Domain.Entities.JourneyShare?> GetByJourneyAndUserAsync(Guid journeyId, Guid userId);

    }
}

