using System.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Movies.Application.Database;

namespace Movies.Api.Health;

public class DatabaseHealthCheck : IHealthCheck
{
	public const string Name = "DatabaseHealthCheck";
	private readonly IDbConnectionFactory _dbConnectionFactory;
	private readonly ILogger<DatabaseHealthCheck> _logger;

	public DatabaseHealthCheck(IDbConnectionFactory dbConnectionFactory, ILogger<DatabaseHealthCheck> logger)
	{
		_dbConnectionFactory = dbConnectionFactory;
		_logger = logger;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
	{
		try
		{
			_ = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
			return HealthCheckResult.Healthy();
		}
		catch (Exception ex)
		{
			const string errorMessage = "Database health check failed";
			_logger.LogError(errorMessage, ex);
			return HealthCheckResult.Unhealthy(errorMessage, ex);
		}
	}
}