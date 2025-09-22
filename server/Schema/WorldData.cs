namespace server.Schema;

public class WorldData
{
    public WorldMetadata Metadata { get; set; }
    public List<WorldImage> ImageList { get; set; }
    public WorldCategory Category { get; set; }
    public WorldDescription Description { get; set; }
    public DateTime DataCreatedAt { get; set; }
    public DateTime FolderUpdatedAt { get; set; }
}