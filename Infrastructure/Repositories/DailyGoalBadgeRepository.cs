using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DailyGoalBadgeRepository : IDailyGoalBadgeRepository
    {
        private readonly AppDbContext _context;

        public DailyGoalBadgeRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Guid> AddAsync(DailyGoalBadge dailyGoalBadge)
        {
            await _context.DailyGoalBadges.AddAsync(dailyGoalBadge);
            await _context.SaveChangesAsync();
            return dailyGoalBadge.Id;
        }

        public void SaveChangesAsync()
        {
            _context.SaveChanges();
        }

        public async Task<List<DailyGoalBadge>> GetAll()
        {
            return await _context.DailyGoalBadges
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DailyGoalBadge?> GetAsyncById(Guid id)
        {
            return await _context.DailyGoalBadges
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task RemoveAsync(DailyGoalBadge dailyGoalBadge)
        {
            _context.DailyGoalBadges.Remove(dailyGoalBadge);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DailyGoalBadge dailyGoalBadge)
        {
            _context.DailyGoalBadges.Update(dailyGoalBadge);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(DailyGoalBadge badge, CancellationToken ct = default)
        {
            await _context.DailyGoalBadges.AddAsync(badge, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsForUserOnDate(Guid userId, DateTime date, CancellationToken ct = default)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            return await _context.DailyGoalBadges
                .AsNoTracking()
                .AnyAsync(b => b.UserId == userId && b.Date >= start && b.Date < end, ct);
        }

        public async Task<bool> ExistsForUserOnDate(Guid userId, DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            return await _context.DailyGoalBadges
                .AsNoTracking()
                .AnyAsync(b => b.UserId == userId && b.Date >= start && b.Date < end);
        }
        public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
    }
}
