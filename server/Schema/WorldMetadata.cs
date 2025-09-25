using server.Core;
using System.Text.Json.Serialization;
namespace server.Schema;

public class WorldMetadata
{
    // Records from VRCWorldMetadata
    public string WorldId { get; private set; }
    public string WorldName { get; private set; }
    public string AuthorId { get; private set; }
    public string AuthorName { get; private set; }
    public string ImageUrl { get; private set; }
    public int Capacity { get; private set; }
    public int Visits { get; private set; }
    public int Favorites { get; private set; }
    public int Heat { get; private set; }
    public int Popularity { get; private set; }
    public List<string> Tags { get; private set; }
    
    // 부가 정보
    public DateTime UpdatedAt { get; private set; }
    
    [JsonIgnore]
    public WorldData WorldData { get; set; } = null!;   // Nav back (옵션)
    
    public WorldMetadata()
    {
    }

    public WorldMetadata(VRCWorldMetadata vrcWorldMetadata)
    {
        WorldId = vrcWorldMetadata.Id;
        WorldName = vrcWorldMetadata.Name;
        AuthorId = vrcWorldMetadata.AuthorId;
        AuthorName = vrcWorldMetadata.AuthorName;
        ImageUrl = vrcWorldMetadata.ImageUrl;
        Capacity = vrcWorldMetadata.Capacity;
        Visits = vrcWorldMetadata.Visits;
        Favorites = vrcWorldMetadata.Favorites;
        Heat = vrcWorldMetadata.Heat;
        Popularity = vrcWorldMetadata.Popularity;
        Tags = vrcWorldMetadata.Tags;
    }

    public WorldMetadata(VRCWorldMetadata vrcWorldMetadata, DateTime updateTime) : this(vrcWorldMetadata)
    {
        UpdatedAt = updateTime;
    }

    public void Update(WorldMetadata other)
    {
        if (WorldId != other.WorldId)
            throw new Exception("W.D.가스터 : 이런 일은 일어날 수 없다 삐리리릭");
        WorldName = other.WorldName;
        AuthorId = other.AuthorId;
        AuthorName = other.AuthorName;
        ImageUrl = other.ImageUrl;
        Capacity = other.Capacity;
        Visits = other.Visits;
        Favorites = other.Favorites;
        Heat = other.Heat;
        Popularity = other.Popularity;
        Tags = other.Tags;
        UpdatedAt = other.UpdatedAt;
    }
}