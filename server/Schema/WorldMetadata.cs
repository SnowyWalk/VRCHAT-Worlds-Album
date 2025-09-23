using server.Core;
namespace server.Schema;

public class WorldMetadata
{
    // Records from VRCWorldMetadata
    public string id { get; private set; }
    public string name { get; private set; }
    public string authorId { get; private set; }
    public string authorName { get; private set; }
    public string imageUrl { get; private set; }
    public int capacity { get; private set; }
    public int visits { get; private set; }
    public int favorites { get; private set; }
    public int heat { get; private set; }
    public int popularity { get; private set; }
    public List<string> tags { get; private set; }
    
    // 부가 정보
    public DateTime UpdatedAt { get; private set; }
    
    public bool IsExpired(TimeSpan ttl)
    {
        return UpdatedAt + ttl <= DateTime.UtcNow;
    }

    public WorldMetadata(VRCWorldMetadata vrcWorldMetadata)
    {
        id = vrcWorldMetadata.Id;
        name = vrcWorldMetadata.Name;
        authorId = vrcWorldMetadata.AuthorId;
        authorName = vrcWorldMetadata.AuthorName;
        imageUrl = vrcWorldMetadata.ImageUrl;
        capacity = vrcWorldMetadata.Capacity;
        visits = vrcWorldMetadata.Visits;
        favorites = vrcWorldMetadata.Favorites;
        heat = vrcWorldMetadata.Heat;
        popularity = vrcWorldMetadata.Popularity;
        tags = vrcWorldMetadata.Tags;
    }

    public WorldMetadata(VRCWorldMetadata vrcWorldMetadata, DateTime updateTime) : this(vrcWorldMetadata)
    {
        UpdatedAt = updateTime;
    }
}