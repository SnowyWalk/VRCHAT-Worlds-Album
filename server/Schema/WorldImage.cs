using System.Text.Json.Serialization;
namespace server.Schema;

public class WorldImage
{
    public string WorldId { get; private set; }
    public string Filename { get; private set; } // PathUtil.ToRelativePath() 적용된 값이어야 함
    public int Width { get; private set; }
    public int Height { get; private set; }
    
    [JsonIgnore]
    public WorldData WorldData { get; set; } = null!;   // Nav back (옵션)

    public WorldImage(string worldId, string filename, int width, int height)
    {
        WorldId = worldId;
        Filename = filename;
        Width = width;
        Height = height;
    }
}