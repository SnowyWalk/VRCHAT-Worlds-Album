namespace server.Schema;

public class WorldImage
{
    public string Key { get; private set; }
    public string ThumbPath { get; private set; }
    public string ViewPath { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
}