using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IDailyGoalBadgeRepository : IRepositoryBase<DailyGoalBadge>
    {
        Task<bool> ExistsForUserOnDate(Guid userId, DateTime date, CancellationToken ct = default);
        Task AddAsync(DailyGoalBadge badge, CancellationToken ct = default);
    }
}

