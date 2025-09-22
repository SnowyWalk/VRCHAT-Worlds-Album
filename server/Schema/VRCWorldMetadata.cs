using System.Text.Json.Serialization;
using System.Collections.Generic;

public class VRCWorldMetadata
{
    [JsonPropertyName("authorId")]
    public string AuthorId { get; set; }

    [JsonPropertyName("authorName")]
    public string AuthorName { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }

    [JsonPropertyName("defaultContentSettings")]
    public object DefaultContentSettings { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("favorites")]
    public int Favorites { get; set; }

    [JsonPropertyName("featured")]
    public bool Featured { get; set; }

    [JsonPropertyName("heat")]
    public int Heat { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; }

    [JsonPropertyName("instances")]
    public List<object> Instances { get; set; }

    [JsonPropertyName("labsPublicationDate")]
    public string LabsPublicationDate { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("occupants")]
    public int Occupants { get; set; }

    [JsonPropertyName("organization")]
    public string Organization { get; set; }

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }

    [JsonPropertyName("previewYoutubeId")]
    public object PreviewYoutubeId { get; set; }

    [JsonPropertyName("privateOccupants")]
    public int PrivateOccupants { get; set; }

    [JsonPropertyName("publicOccupants")]
    public int PublicOccupants { get; set; }

    [JsonPropertyName("publicationDate")]
    public string PublicationDate { get; set; }

    [JsonPropertyName("recommendedCapacity")]
    public int RecommendedCapacity { get; set; }

    [JsonPropertyName("releaseStatus")]
    public string ReleaseStatus { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("thumbnailImageUrl")]
    public string ThumbnailImageUrl { get; set; }

    [JsonPropertyName("udonProducts")]
    public List<object> UdonProducts { get; set; }

    [JsonPropertyName("unityPackages")]
    public List<object> UnityPackages { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }

    [JsonPropertyName("urlList")]
    public List<object> UrlList { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("visits")]
    public int Visits { get; set; }
}