namespace server.Schema;

public class WorldData
{
    public string WorldId { get; set; }
    public WorldMetadata? Metadata { get; set; } = null;
    public List<WorldImage> ImageList { get; private set; } = new();
    public List<WorldCategory> CategoryList { get; set; } = new();
    public WorldDescription? Description { get; set; } = null;
    public DateTime DataCreatedAt { get; set; }
    public DateTime LastFolderModifiedAt { get; set; } = DateTime.MinValue;
}