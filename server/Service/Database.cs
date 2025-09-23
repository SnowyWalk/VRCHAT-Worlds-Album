using server.Core;
using server.Schema;
using System.Text.Json;

namespace server.Service;

public class Database
{
    private static Database m_instance;
    public static Database Instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new();
            return m_instance;
        }
    }

    private Dictionary<string, WorldData> m_data = new();

    public List<WorldData> GetWorldDataListByPaging(int page = 0, int pageCount = 10)
    {
        List<WorldData> result;
        lock (m_data)
        {
            result = m_data.Values.OrderByDescending(key => key.DataCreatedAt).Skip(page * pageCount).Take(pageCount).ToList();
        }
        return result;
    }

    public void LoadFromFile()
    {
        lock (m_data)
            m_data.Clear();

        if (File.Exists(Config.DatabaseJsonPath) == false)
        {
            Log.Info($"[Database.LoadFromFile] 파일이 없어서 로드 실패: {Config.DatabaseJsonPath}");
            return;
        }

        using FileStream openStream = File.OpenRead(Config.DatabaseJsonPath);
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
        using FileStream openStream = File.OpenWrite(Config.DatabaseJsonTempPath);
        lock (m_data)
            JsonSerializer.Serialize(openStream, m_data);
        File.Move(Config.DatabaseJsonTempPath, Config.DatabaseJsonPath, overwrite: true);
    }

    public bool HasKey(string worldId)
    {
        lock (m_data)
            return m_data.ContainsKey(worldId);
    }

    public WorldData? GetWorldData(string worldId)
    {
        lock (m_data)
            return m_data.GetValueOrDefault(worldId);
    }

    public WorldData AddWorldData(string worldId, DateTime createdAt)
    {
        WorldData newWorld = new WorldData() {
            DataCreatedAt = createdAt,
        };

        lock (m_data)
            m_data[worldId] = newWorld;

        SaveToFile();
        return newWorld;
    }

    public void UpdateWorldMetaData(string worldId, WorldMetadata worldMetadata)
    {
        lock (m_data)
            m_data[worldId].Metadata = worldMetadata;

        SaveToFile();
    }
}