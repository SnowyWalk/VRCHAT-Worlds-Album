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
        return await m_db.Data
            .Include(e => e.ImageList)
            .Include(e => e.Metadata)
            .Include(e => e.CategoryList)
            .Include(e => e.Description)
            .OrderByDescending(e => e.DataCreatedAt)
            .ThenBy(e => e.WorldId)
            .Take(pageCount)
            .ToListAsync();
    }

    public async Task<List<WorldData>> GetWorldDataListAfterCursor(DateTime cursorDateTime, string subKey, int pageCount = 10)
    {
        return await m_db.Data
            .Include(e => e.ImageList)
            .Include(e => e.Metadata)
            .Include(e => e.CategoryList)
            .Include(e => e.Description)
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
        WorldData newWorld = new WorldData()
        {
            WorldId = worldId,
            DataCreatedAt = createdAt,
        };
        await m_db.Data.AddAsync(newWorld);
        await m_db.SaveChangesAsync();
    }

    public async Task<DateTime> GetLastFolderModifiedTime(string worldId)
    {
        var worldData = await m_db.Data
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.WorldId == worldId);
        if (worldData is null)
            throw new Exception($"[GetLastFolderModifiedTime] 없는 WorldId에 대한 쿼리: {worldId}");
        return worldData.LastFolderModifiedAt;
    }

    public async Task UpdateLastFolderModifiedTime(string worldId, DateTime modifiedAt)
    {
        await m_db.Data
            .Where(e => e.WorldId == worldId)
            .ExecuteUpdateAsync(e =>
                e.SetProperty(x => x.LastFolderModifiedAt, _ => modifiedAt));
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

        bool isAlreadyFresh = await m_db.Metadata
            .AsNoTracking()
            .AnyAsync(e => e.WorldId == worldId && e.UpdatedAt > threshold);
        return isAlreadyFresh == false;
    }

    #endregion

    #region WorldImage

    public async Task<List<string>> GetWorldImageFileNameList(string worldId)
    {
        return await m_db.Image
            .AsNoTracking()
            .Where(e => e.WorldId == worldId)
            .Select(e => e.Filename)
            .ToListAsync();
    }

    public async Task RemoveWorldImage(string worldId, string removedImageFilename)
    {
        await m_db.Image
            .Where(e => e.WorldId == worldId && e.Filename == removedImageFilename)
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

    #region WorldCategory

    public async Task<List<WorldCategory>> GetWorldDataCategoryList(string worldId)
    {
        return await m_db.Data
            .AsNoTracking()
            //.Include(e => e.CategoryList)
            .Where(e => worldId == e.WorldId)
            .Select(e => e.CategoryList)
            .FirstAsync();
    }

    public async Task<Dictionary<string, List<WorldCategory>>> GetWorldDataCategoryList(string[] worldIdList)
    {
        return await m_db.Data
            .AsNoTracking()
            .Include(e => e.CategoryList)
            .Where(e => worldIdList.Contains(e.WorldId))
            .ToDictionaryAsync(e => e.WorldId, e => e.CategoryList);
    }

    public async Task UpdateWorldDataCategoryList(string worldId, List<string> reqCategoryNameList)
    {
        // 0) 이름 정규화 + 중복 제거
        static string Norm(string s) => (s ?? string.Empty).Trim();
        var reqNormSet = new HashSet<string>(
            reqCategoryNameList.Select(Norm).Where(x => x.Length > 0),
            StringComparer.OrdinalIgnoreCase
        );
        if (reqNormSet.Count == 0)
            return;

        using var tx = await m_db.Database.BeginTransactionAsync();

        // 1) 존재하는 카테고리 먼저 조회
        var exist = await m_db.Category
            .Where(c => reqNormSet.Contains(c.Name))
            .ToListAsync();

        // 2) 없는 이름만 추려서 생성
        var existNameSet = new HashSet<string>(exist.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        var toCreateNames = reqNormSet.Except(existNameSet).ToList();
        if (toCreateNames.Count > 0)
        {
            await m_db.Category.AddRangeAsync(toCreateNames.Select(n => new WorldCategory(n)));
            try
            {
                await m_db.SaveChangesAsync(); // ← 반드시 저장해야 다음 조회에 반영됨
            }
            catch (DbUpdateException)
            {
                // UNIQUE(name) 경합 등 발생 시 무시하고 재조회로 수습
            }

            // 생성 포함 최종 집합 재조회
            exist = await m_db.Category
                .Where(c => reqNormSet.Contains(c.Name))
                .ToListAsync();
        }

        // 3) 대상 World 로드(+현 연결)
        var world = await m_db.Data
            .Include(w => w.CategoryList)
            .FirstOrDefaultAsync(w => w.WorldId == worldId);

        if (world == null)
            throw new InvalidOperationException($"World not found: {worldId}");

        // 4) 델타 계산 (추가/삭제만 수행)
        var targetIds = new HashSet<int>(exist.Select(c => c.Id));
        var currentIds = new HashSet<int>(world.CategoryList.Select(c => c.Id));

        var toAddIds = targetIds.Except(currentIds).ToList();
        var toRemoveIds = currentIds.Except(targetIds).ToList();

        if (toRemoveIds.Count > 0)
            world.CategoryList.RemoveAll(c => toRemoveIds.Contains(c.Id)); // List<T>라면 확장메서드 사용 가능

        if (toAddIds.Count > 0)
        {
            // 추적 안 된 엔티티는 키만 맞으면 Attach되어 연결됨
            var toAddEntities = exist.Where(c => toAddIds.Contains(c.Id)).ToList();
            world.CategoryList.AddRange(toAddEntities);
        }

        await m_db.SaveChangesAsync();
        await tx.CommitAsync();
    }


    #endregion

    #region Description

    public async Task<WorldDescription?> GetWorldDescription(string worldId)
    {
        return await m_db.Description
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.WorldId == worldId);
    }

    public async Task UpdateWorldDescription(string worldId, string description)
    {
        if (await m_db.Description.AsNoTracking().AnyAsync(e => e.WorldId == worldId))
            await m_db.Description.Where(e => e.WorldId == worldId).ExecuteUpdateAsync(e => e.SetProperty(e => e.Description, description));
        else
        {
            await m_db.Description.AddAsync(new WorldDescription(worldId, description));
            await m_db.SaveChangesAsync();
        }
    }

    #endregion
}