using Microsoft.Extensions.Options;
using server.Core;
namespace server.Service;

public class StartupOrchestratorService : BackgroundService
{
    private readonly WorldPreprocessor m_worldPreprocessor;
    private readonly Database m_database;
    private readonly CacheOptions m_cacheOptions;

    public StartupOrchestratorService(
        Database database, 
        WorldPreprocessor worldPreprocessor,
        IOptions<CacheOptions> cacheOptions)
    {
        m_database = database;
        m_worldPreprocessor = worldPreprocessor;
        m_cacheOptions = cacheOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        m_database.LoadFromFile();
        
        while (stoppingToken.IsCancellationRequested == false)
        {
            await m_worldPreprocessor.Scan(stoppingToken);
            
            await Task.Delay(m_cacheOptions.ScanInterval, stoppingToken);
        }
    }
}