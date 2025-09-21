using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class SqlConnectionHealthCheck : IHealthCheck
{
    private readonly string _connString;
    public SqlConnectionHealthCheck(IConfiguration cfg)
        => _connString = cfg.GetConnectionString("DefaultConnection")!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connString);
            await conn.OpenAsync(ct);
            return HealthCheckResult.Healthy("SQL reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL unreachable", ex);
        }
    }
}
