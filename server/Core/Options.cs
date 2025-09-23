public sealed class AppPathsOptions
{
    public string? BaseDir { get; set; }
    public string ThumbImageDir { get; set; } = "";
    public string ViewImageDir { get; set; } = "";
    public string DatabaseJsonPath { get; set; } = "";
    public string DatabaseJsonTempPath { get; set; } = "";
    public string ScanFolderPath { get; set; } = "";
}

public sealed class ImageOptions
{
    public int ThumbQuality { get; set; } = 15;
    public int ViewQuality  { get; set; } = 95;
}

public sealed class CacheOptions
{
    public TimeSpan WorldMetadataTTL { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan ScanInterval     { get; set; } = TimeSpan.FromMinutes(1);
}