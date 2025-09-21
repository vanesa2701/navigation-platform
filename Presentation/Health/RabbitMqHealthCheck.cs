using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _host; private readonly string _user; private readonly string _pass;
    public RabbitMqHealthCheck(IConfiguration cfg)
    {
        _host = cfg["RabbitMq:HostName"] ?? "localhost";
        _user = cfg["RabbitMq:UserName"] ?? "guest";
        _pass = cfg["RabbitMq:Password"] ?? "guest";
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = _host, UserName = _user, Password = _pass };
            using var conn = factory.CreateConnection();
            using var ch = conn.CreateModel();
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ reachable"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ unreachable", ex));
        }
    }
}
