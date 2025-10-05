using System.Text.Json.Serialization;

namespace server.Schema;

public class WorldCategory
{
    public int Id { get; private set; } // Auto Increment, UUID
    public string Name { get; private set; } = null!; // Key (all, game_horror, chill, ...)
    public LocalizeText LocalizedText { get; set; }
    // public bool ShowInWorldCard { get; set; }
    // public bool ShowInCategoryMenu { get; set; }

    [JsonIgnore]
    public List<WorldData> WorldDataList { get; set; } = new();

    public WorldCategory() { }
    public WorldCategory(string name) { Name = name; }
}