using System.Text.Json.Serialization;

namespace server.Schema;

public class WorldDescription
{
    public string WorldId { get; set; }
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public WorldData WorldData { get; private set; }

    public WorldDescription() { }
    public WorldDescription(string worldId, string description) { WorldId = worldId; Description = description; }
}