namespace server.Schema;

public class WorldData
{
    public WorldMetadata? Metadata { get; set; } = null;
    public List<WorldImage>? ImageList { get; set; } = null;
    public WorldCategory? Category { get; set; } = null;
    public WorldDescription? Description { get; set; } = null;
    public DateTime DataCreatedAt { get; set; }
    public DateTime FolderUpdatedAt { get; set; }
}