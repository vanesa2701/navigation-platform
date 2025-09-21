using Application.Services.Messaging;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class DailyGoalWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyGoalWorker> _logger;
        private readonly TimeSpan _delay = TimeSpan.FromMinutes(30);
        private readonly TimeZoneInfo _tz;

        public DailyGoalWorker(IServiceProvider sp, ILogger<DailyGoalWorker> logger, IConfiguration cfg)
        {
            _serviceProvider = sp;
            _logger = logger;
            var tzId = cfg["DailyGoal:TimeZone"] ?? "UTC"; // e.g. Europe/Tirane
            _tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyGoalWorker started at {Time}", DateTimeOffset.UtcNow);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var journeyRepo = scope.ServiceProvider.GetRequiredService<IJourneyRepository>();
                    var badgeRepo = scope.ServiceProvider.GetRequiredService<IDailyGoalBadgeRepository>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                    var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _tz);
                    var day = nowLocal.Date; // local calendar day
                    var journeysToday = await journeyRepo.GetJourneysForDateAsync(day, stoppingToken); // add CT overload

                    foreach (var userGroup in journeysToday
                             .OrderBy(j => j.UserId)
                             .ThenBy(j => j.ArrivalTime))
                    {
                        // grouped enumeration without extra allocations
                    }

                    var byUser = journeysToday.GroupBy(j => j.UserId);
                    foreach (var g in byUser)
                    {
                        if (await badgeRepo.ExistsForUserOnDate(g.Key, day, stoppingToken))
                            continue;

                        double total = 0;
                        Journey? trigger = null;
                        foreach (var j in g.OrderBy(x => x.ArrivalTime))
                        {
                            total += (double)j.RouteDistanceKm;
                            if (total >= 20.0) { trigger = j; break; }
                        }

                        if (trigger is not null)
                        {
                            trigger.IsDailyGoalAchieved = true;
                            await journeyRepo.UpdateAsync(trigger, stoppingToken);

                            await badgeRepo.AddAsync(new DailyGoalBadge
                            {
                                Id = Guid.NewGuid(),
                                UserId = g.Key,
                                Date = day,
                                TotalDistanceKm = total
                            }, stoppingToken);

                            await publisher.PublishAsync(
                                "DailyGoalAchieved",
                                new DailyGoalAchieved(g.Key, day),
                                stoppingToken);

                            _logger.LogInformation("Badge awarded to {User} for journey {Journey}", g.Key, trigger.JourneyId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DailyGoalWorker");
                }

                await Task.Delay(_delay, stoppingToken);
            }
            _logger.LogInformation("DailyGoalWorker stopping at {Time}", DateTimeOffset.UtcNow);
        }
    }


    public class DailyGoalAchieved
    {
        public Guid UserId { get; }
        public DateTime Date { get; }

        public DailyGoalAchieved(Guid userId, DateTime date)
        {
            UserId = userId;
            Date = date;
        }
    }
}

