using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Core;
using server.Schema;

namespace server.Service;

public class Database
{
    private readonly CacheOptions m_cacheOptions;
    private readonly DB m_db;

    public Database(IOptions<CacheOptions> cacheOptions, DB db)
    {
        m_cacheOptions = cacheOptions.Value;
        m_db = db;
    }

    #region API

    public async Task<List<WorldData>> GetWorldDataListFirstPage(int pageCount = 10)
    {
        return await m_db.World
            .OrderByDescending(e => e.DataCreatedAt)
            .ThenBy(e => e.WorldId)
            .Take(pageCount)
            .ToListAsync();
    }

    public async Task<List<WorldData>> GetWorldDataListAfterCursor(DateTime cursorDateTime, string subKey, int pageCount = 10)
    {
        return await m_db.World
            .Where(e => e.DataCreatedAt < cursorDateTime ||
                e.DataCreatedAt == cursorDateTime && string.Compare(e.WorldId, subKey) > 0)
            .OrderByDescending(e => e.DataCreatedAt)
            .ThenBy(e => e.WorldId)
            .Take(pageCount)
            .ToListAsync();
    }

    #endregion

    #region WorldData

    public async Task<bool> HasWorldData(string worldId)
    {
        return await m_db.World.AnyAsync(e => e.WorldId == worldId);
    }

    public async Task AddWorldData(string worldId, DateTime createdAt)
    {
        WorldData newWorld = new WorldData() {
            WorldId = worldId,
            DataCreatedAt = createdAt,
        };
        await m_db.World.AddAsync(newWorld);
        await m_db.SaveChangesAsync();
    }

    public async Task<DateTime> GetLastFolderModifiedTime(string worldId)
    {
        var worldData = await m_db.World
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.WorldId == worldId);
        if (worldData is null)
            throw new Exception($"[GetLastFolderModifiedTime] 없는 WorldId에 대한 쿼리: {worldId}");
        return worldData.LastFolderModifiedAt;
    }

    public async Task UpdateLastFolderModifiedTime(string worldId, DateTime modifiedAt)
    {
        await m_db.World
            .Where(e => e.WorldId == worldId)
            .ExecuteUpdateAsync(e =>
                e.SetProperty(x => x.LastFolderModifiedAt, modifiedAt));
    }

    #endregion

    #region WorldMetadata

    public async Task UpdateWorldMetaData(WorldMetadata worldMetadata)
    {
        WorldMetadata? existWorldMetadata = await m_db.Metadata.FindAsync(worldMetadata.WorldId);
        if (existWorldMetadata is null)
            await m_db.Metadata.AddAsync(worldMetadata); // 신규
        else
            m_db.Entry(existWorldMetadata).CurrentValues.SetValues(worldMetadata); // 스칼라 필드 일괄 복사
        await m_db.SaveChangesAsync();
    }

    public async Task<bool> IsWorldMetadataNeedToUpdate(string worldId)
    {
        TimeSpan ttl = m_cacheOptions.WorldMetadataTTL;
        var threshold = DateTime.UtcNow - ttl;

        bool isAlreadyFresh = await m_db.Metadata.AsNoTracking().AnyAsync(e => e.WorldId == worldId && e.UpdatedAt > threshold);
        return isAlreadyFresh == false;
    }

    #endregion

    #region WorldImage

    public async Task<List<string>> GetWorldImageFileNameList(string worldId)
    {
        return await m_db.World
            .AsNoTracking()
            .Where(e => e.WorldId == worldId)
            .SelectMany(e => e.ImageList.Select(e => e.Filename))
            .ToListAsync();
    }

    public async Task RemoveWorldImage(string worldId, string removedImageFilename)
    {
        await m_db.World
            .Where(e => e.WorldId == worldId)
            .SelectMany(e => e.ImageList.Where(x => x.Filename == removedImageFilename))
            .ExecuteDeleteAsync();
    }

    public async Task<bool> HasWorldImage(string worldId, string filename)
    {
        return await m_db.Image
            .AsNoTracking()
            .AnyAsync(e => e.WorldId == worldId && e.Filename == filename);
    }

    // From Background worker
    public async Task AddWorldImage(string worldId, string filename, int width, int height)
    {
        await m_db.Image.AddAsync(new WorldImage(worldId, filename, width, height));
        await m_db.SaveChangesAsync();
    }

    #endregion

}