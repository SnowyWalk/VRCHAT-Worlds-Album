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
        {
            m_data.Clear();
        }

        if (File.Exists(Config.DatabaseJsonPath) == false)
        {
            Log.Info($"[Database.LoadFromFile] 파일이 없어서 로드 실패: {Config.DatabaseJsonPath}");
            return;
        }

        // 파일을 스트림으로 열어 메모리 사용량을 최소화합니다.
        using FileStream openStream = File.OpenRead(Config.DatabaseJsonPath);

        // 스트림에서 직접 역직렬화를 수행합니다.
        var tempData = JsonSerializer.Deserialize<Dictionary<string, WorldData>>(openStream);

        lock (m_data)
        {
            if (tempData != null)
            {
                m_data = tempData;
            }
            else
            {
                m_data.Clear();
            }
        }
    }
}