using Microsoft.Extensions.Options;
namespace server.Util;

public interface IPathUtil
{
    string GetOriginImagePath(string worldId, string filename);
    string GetThumbImagePath(string worldId, string filename);
    string GetViewImagePath(string worldId, string filename);
}

public sealed class PathUtil : IPathUtil
{
    private readonly AppPathsOptions m_paths;
    private readonly string m_contentRootPath;

    public PathUtil(IOptions<AppPathsOptions> paths, IHostEnvironment env)
    {
        m_paths = paths.Value;
        m_contentRootPath = env.ContentRootPath;
    }

    // public string ToRelativePath(string path)
    //     => Path.GetRelativePath(_baseDir, path);

    public string GetOriginImagePath(string worldId, string filename)
        => Path.Combine(m_contentRootPath, m_paths.OriginImageDir, worldId, Path.GetFileName(filename));

    public string GetThumbImagePath(string worldId, string filename)
        => Path.Combine(m_contentRootPath, m_paths.ThumbImageDir, worldId, Path.ChangeExtension(Path.GetFileName(filename), ".webp"));

    public string GetViewImagePath(string worldId, string filename)
        => Path.Combine(m_contentRootPath, m_paths.ViewImageDir, worldId, Path.ChangeExtension(Path.GetFileName(filename), ".webp"));
}