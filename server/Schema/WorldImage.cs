namespace server.Schema;

public class WorldImage
{
    public string SourcePath { get; private set; } // PathUtil.ToRelativePath() 적용된 값이어야 함
    public string ThumbPath { get; private set; } // PathUtil.ToRelativePath() 적용된 값이어야 함
    public string ViewPath { get; private set; } // PathUtil.ToRelativePath() 적용된 값이어야 함
    public int Width { get; private set; }
    public int Height { get; private set; }

    public WorldImage(string sourcePath, string thumbPath, string viewPath, int width, int height)
    {
        SourcePath = sourcePath;
        ThumbPath = thumbPath;
        ViewPath = viewPath;
        Width = width;
        Height = height;
    }

    public string Key => SourcePath;
}