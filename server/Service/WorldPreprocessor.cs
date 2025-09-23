using server.Core;
using server.Schema;
namespace server.Service;

public class WorldPreprocessor
{
    private Queue<(string worldId, string path)> m_imageProcessingQueue = new();

    public async Task Scan()
    {
        DirectoryInfo currentDir = new DirectoryInfo(Config.ScanFolderPath);
        foreach (DirectoryInfo worldFolder in currentDir.EnumerateDirectories())
        {
            string worldId = worldFolder.Name;

            DateTime modifiedAt = worldFolder.LastWriteTimeUtc;
            DateTime createdAt = worldFolder.CreationTimeUtc != modifiedAt ? worldFolder.CreationTimeUtc : DateTime.UtcNow; // BirthTime이 없으면 현재 시각으로 생성

            // Process WorldMetadata
            WorldData worldData = Database.Instance.GetWorldData(worldId) ?? Database.Instance.AddWorldData(worldId, createdAt);
            bool needToUpdate = worldData.Metadata == null || worldData.Metadata.IsExpired;
            if (needToUpdate)
            {
                WorldMetadata? worldMetadata = await VRCClient.FetchVRCWorldMetadata(worldId);
                if (worldMetadata != null)
                    Database.Instance.UpdateWorldMetaData(worldId, worldMetadata);
            }

            // Process Image




        }
    }

    private
}