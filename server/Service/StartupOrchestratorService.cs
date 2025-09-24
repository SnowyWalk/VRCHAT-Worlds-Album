using Microsoft.Extensions.Options;
using server.Core;
namespace server.Service;

public class StartupOrchestratorService : BackgroundService
{
    private readonly WorldPreprocessor m_worldPreprocessor;
    private readonly CacheOptions m_cacheOptions;

    public StartupOrchestratorService(
        WorldPreprocessor worldPreprocessor,
        IOptions<CacheOptions> cacheOptions)
    {
        m_worldPreprocessor = worldPreprocessor;
        m_cacheOptions = cacheOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            await m_worldPreprocessor.Scan(stoppingToken);
            
            await Task.Delay(m_cacheOptions.ScanInterval, stoppingToken);
        }
    }
}