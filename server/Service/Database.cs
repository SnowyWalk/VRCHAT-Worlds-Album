using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        return await m_db.Data
            .OrderByDescending(e => e.DataCreatedAt)
            .ThenBy(e => e.WorldId)
            .Take(pageCount)
            .ToListAsync();
    }

    public async Task<List<WorldData>> GetWorldDataListAfterCursor(DateTime cursorDateTime, string subKey, int pageCount = 10)
    {
        return await m_db.Data
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
        return await m_db.Data.AnyAsync(e => e.WorldId == worldId);
    }

    public async Task AddWorldData(string worldId, DateTime createdAt)
    {
        WorldData newWorld = new WorldData() {
            WorldId = worldId,
            DataCreatedAt = createdAt,
        };
        await m_db.Data.AddAsync(newWorld);
    }

    public async Task<DateTime> GetLastFolderModifiedTime(string worldId)
    {
        DateTime lastFolderModifiedAt = await m_db.Data.AsNoTracking().Where(e => e.WorldId == worldId).Select(e => e.LastFolderModifiedAt).SingleOrDefaultAsync();
        if (lastFolderModifiedAt == default(DateTime))
            throw new Exception($"[GetLastFolderModifiedTime] 없는 WorldId에 대한 쿼리: {worldId}");
        return lastFolderModifiedAt;
    }

    public async Task UpdateLastFolderModifiedTime(string worldId, DateTime modifiedAt)
    {
        await m_db.Data
            .Where(e => e.WorldId == worldId)
            .ExecuteUpdateAsync(e =>
                e.SetProperty(x => x.LastFolderModifiedAt, modifiedAt));
    }

    #endregion

    #region WorldMetadata

    public async Task UpdateWorldMetaData(string worldId, WorldMetadata worldMetadata)
    {
        await m_db.Data
            .Where(e => e.WorldId == worldId)
            .ExecuteUpdateAsync(e =>
                e.SetProperty(x => x.Metadata, worldMetadata));
    }

    public async Task<bool> IsWorldMetadataNeedToUpdate(string worldId)
    {
        WorldData? worldData = await m_db.Data
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.WorldId == worldId);

        if (worldData is null)
            throw new Exception($"[IsWorldMetadataNeedToUpdate] 없는 WorldId에 대한 쿼리: {worldId}");

        return worldData.Metadata == null || worldData.Metadata.IsExpired(m_cacheOptions.WorldMetadataTTL);
    }

    #endregion

    #region WorldImage

    public async Task<List<string>> GetWorldImageFileNameList(string worldId)
    {
        return await m_db.Data
            .AsNoTracking()
            .Where(e => e.WorldId == worldId)
            .SelectMany(e => e.ImageList.Select(e => e.Filename))
            .ToListAsync();
    }

    public async Task RemoveWorldImage(string worldId, string removedImageFilename)
    {
        await m_db.Data.Where(e => e.WorldId == worldId).SelectMany(e => e.ImageList.Where(x => x.Filename == removedImageFilename)).ExecuteDeleteAsync();
    }

    public async Task AddWorldImage(string worldId, string filename, int width, int height)
    {
        WorldData? worldData = await m_db.Data.SingleOrDefaultAsync(e => e.WorldId == worldId);
        if (worldData is null)
            throw new Exception($"[AddWorldImage] 없는 WorldId에 대한 쿼리: {worldId} / {filename}");
        worldData.ImageList.Add(new WorldImage(worldId, filename, width, height));
        await m_db.SaveChangesAsync();
    }

    #endregion

}