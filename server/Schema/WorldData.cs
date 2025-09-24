namespace server.Schema;

public class WorldData
{
    public WorldMetadata? Metadata { get; set; } = null;
    public Dictionary<string, WorldImage> ImageDic { get; private set; } = new ();
    public WorldCategory? Category { get; set; } = null;
    public WorldDescription? Description { get; set; } = null;
    public DateTime DataCreatedAt { get; set; }
    public DateTime LastFolderModifiedAt { get; set; } = DateTime.MinValue;
}