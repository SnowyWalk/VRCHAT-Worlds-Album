using Microsoft.Extensions.Options;
namespace server.Util;

public interface IPathUtil
{
    string ToRelativePath(string path);
    string GetThumbPath(string worldId, string srcPath);
    string GetViewPath(string worldId, string srcPath);
}

public sealed class PathUtil : IPathUtil
{
    private readonly AppPathsOptions _paths;
    private readonly string _baseDir;

    public PathUtil(IOptions<AppPathsOptions> paths, IHostEnvironment env)
    {
        _paths = paths.Value;
        // BaseDir이 설정 안 됐으면 콘텐츠 루트를 기본값으로
        _baseDir = string.IsNullOrWhiteSpace(_paths.BaseDir)
            ? env.ContentRootPath
            : _paths.BaseDir;
    }

    public string ToRelativePath(string path)
        => Path.GetRelativePath(_baseDir, path);

    public string GetThumbPath(string worldId, string srcPath)
        => Path.Combine(_paths.ThumbImageDir, worldId, Path.ChangeExtension(Path.GetFileName(srcPath), ".webp"));

    public string GetViewPath(string worldId, string srcPath)
        => Path.Combine(_paths.ViewImageDir, worldId, Path.ChangeExtension(Path.GetFileName(srcPath), ".webp"));
}