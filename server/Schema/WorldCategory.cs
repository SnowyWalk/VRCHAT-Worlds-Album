using System.Text.Json.Serialization;

namespace server.Schema;

public class WorldCategory
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;

    [JsonIgnore]
    public List<WorldData> WorldDataList { get; set; } = new();
}