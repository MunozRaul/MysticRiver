using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MysticRiver.HttpApi.Battles;

public sealed class TokenSweeperService : BackgroundService {
    private readonly IConnectionMapping _mapping;
    private readonly ILogger<TokenSweeperService> _logger;
    private readonly TimeSpan _sweepInterval;

    public TokenSweeperService(IConnectionMapping mapping, ILogger<TokenSweeperService> logger, TimeSpan? sweepInterval = null) {
        _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sweepInterval = sweepInterval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("TokenSweeperService started.");
        while (!stoppingToken.IsCancellationRequested) {
            try {
                _mapping.RemoveExpiredTokens();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error sweeping expired tokens.");
            }

            try {
                await Task.Delay(_sweepInterval, stoppingToken);
            }
            catch (TaskCanceledException) {
                // ignore - shutting down
            }
        }

        _logger.LogInformation("TokenSweeperService stopping.");
    }
}
