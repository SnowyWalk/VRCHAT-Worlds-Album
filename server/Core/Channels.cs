namespace server.Core;

public static class Channels
{
    public record ImageJob(string worldId, string SourceImageFilename);
    
}