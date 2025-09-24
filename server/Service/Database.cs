using Microsoft.Extensions.Options;
using server.Core;
using server.Schema;
using System.Text.Json;

namespace server.Service;

public class Database
{
    private Dictionary<string, WorldData> m_data = new();
    private readonly AppPathsOptions m_appPathOption;
    private readonly ImageOptions m_imageOptions;
    private readonly CacheOptions m_cacheOptions;

    public Database(IOptions<AppPathsOptions> appPathOption, IOptions<ImageOptions> imageOptions, IOptions<CacheOptions> cacheOptions)
    {
        m_appPathOption = appPathOption.Value;
        m_imageOptions = imageOptions.Value;
        m_cacheOptions = cacheOptions.Value;
    }

    #region API

    public List<WorldData> GetWorldDataListByPaging(int page = 0, int pageCount = 10)
    {
        List<WorldData> result;
        lock (m_data)
        {
            result = m_data.Values.OrderByDescending(key => key.DataCreatedAt).Skip(page * pageCount).Take(pageCount).ToList();
        }
        return result;
    }

    #endregion

    #region File

    public void LoadFromFile()
    {
        lock (m_data)
            m_data.Clear();

        if (File.Exists(m_appPathOption.DatabaseJsonPath) == false)
        {
            Log.Info($"[Database.LoadFromFile] 파일이 없어서 로드 실패: {m_appPathOption.DatabaseJsonPath}");
            return;
        }

        using FileStream openStream = File.OpenRead(m_appPathOption.DatabaseJsonPath);
        var tempData = JsonSerializer.Deserialize<Dictionary<string, WorldData>>(openStream);

        lock (m_data)
        {
            if (tempData != null)
                m_data = tempData;
            else
                m_data.Clear();
        }
    }

    public void SaveToFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(m_appPathOption.DatabaseJsonTempPath)!);
        using (FileStream openStream = File.Create(m_appPathOption.DatabaseJsonTempPath))
        {
            lock (m_data)
                JsonSerializer.Serialize(openStream, m_data);
        }
        File.Move(m_appPathOption.DatabaseJsonTempPath, m_appPathOption.DatabaseJsonPath, overwrite: true);
    }

    #endregion

    #region WorldData

    public bool HasWorld(string worldId)
    {
        lock (m_data)
            return m_data.ContainsKey(worldId);
    }

    public void AddWorldData(string worldId, DateTime createdAt)
    {
        WorldData newWorld = new WorldData() {
            DataCreatedAt = createdAt,
        };

        lock (m_data)
            m_data[worldId] = newWorld;

        SaveToFile();
    }

    public DateTime GetLastFolderModifiedTime(string worldId)
    {
        lock (m_data)
        {
            if (m_data.TryGetValue(worldId, out WorldData? worldData) == false)
                throw new Exception($"[GetLastFolderModifiedTime] 없는 WorldId에 대한 쿼리: {worldId}");

            return worldData.LastFolderModifiedAt;
        }
    }

    public void UpdateLastFolderModifiedTime(string worldId, DateTime modifiedAt)
    {
        lock (m_data)
        {
            if (m_data.TryGetValue(worldId, out WorldData? worldData) == false)
                throw new Exception($"[UpdateLastFolderModifiedTime] 없는 WorldId에 대한 쿼리: {worldId}");

            worldData.LastFolderModifiedAt = modifiedAt;
        }
        SaveToFile();
    }

    #endregion

    #region WorldMetadata

    public void UpdateWorldMetaData(string worldId, WorldMetadata worldMetadata)
    {
        lock (m_data)
            m_data[worldId].Metadata = worldMetadata;

        SaveToFile();
    }

    public bool IsWorldMetadataNeedToUpdate(string worldId)
    {
        lock (m_data)
        {
            if (m_data.TryGetValue(worldId, out WorldData? worldData) == false)
                throw new Exception($"[IsWorldMetadataNeedToUpdate] 없는 WorldId에 대한 쿼리: {worldId}");

            return worldData.Metadata == null || worldData.Metadata.IsExpired(m_cacheOptions.WorldMetadataTTL);
        }
    }

    #endregion

    #region WorldImage

    public List<string> GetWorldImagePathList(string worldId)
    {
        List<string> result = new();
        lock (m_data)
        {
            if (m_data.TryGetValue(worldId, out WorldData? worldData) == false)
                return result;

            worldData.ImageDic.Keys.Order().ToList().ForEach(result.Add);
        }
        return result;
    }

    public void RemoveWorldImage(string worldId, string removedImagePath)
    {
        lock (m_data)
        {
            if (m_data.TryGetValue(worldId, out WorldData? worldData) == false)
                throw new Exception($"[RemoveWorldImage] 없는 WorldId에 대한 쿼리: {worldId}");

            if (worldData.ImageDic.ContainsKey(removedImagePath) == false)
                throw new Exception($"[RemoveWorldImage] 없는 ImagePath에 대한 쿼리: {worldId} / {removedImagePath}");

            worldData.ImageDic.Remove(removedImagePath);
        }
        SaveToFile();
    }

    public void AddWorldImage(string worldId, string sourcePath, string thumbPath, string viewPath, int width, int height)
    {
        lock (m_data)
        {
            if (m_data.TryGetValue(worldId, out WorldData? worldData) == false)
                throw new Exception($"[AddWorldImage] 없는 WorldId에 대한 쿼리: {worldId} / {sourcePath}");

            if(worldData.ImageDic.ContainsKey(sourcePath))
                Log.Error($"[AddWorldImage] 이미 있는데 이미지 정보를 추가하려한다: {worldId} / {sourcePath}");

            worldData.ImageDic[sourcePath] = new WorldImage(sourcePath, thumbPath, viewPath, width, height);
        }
        SaveToFile();
    }
    
    #endregion

}