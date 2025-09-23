using Microsoft.Extensions.Options;
using server.Core;
using server.Schema;
using server.Util;
using System.Diagnostics;
using System.Threading.Channels;
namespace server.Service;

public class WorldPreprocessor
{
    private readonly Channel<Channels.ImageJob> m_imageJobChannel;
    private readonly Database m_database;
    private readonly VRCClient m_vrcClient;
    private readonly AppPathsOptions m_appPathsOptions;
    private readonly IPathUtil m_pathUtil;
    public bool IsScanWorking { get; private set; }

    private static List<string> m_cachedStringList = new();

    public WorldPreprocessor(
        Channel<Channels.ImageJob> imageJobChannel, 
        Database database, 
        VRCClient vrcClient, 
        IOptions<AppPathsOptions> appPathsOption,
        IPathUtil pathUtil)
    {
        m_imageJobChannel = imageJobChannel;
        m_database = database;
        m_vrcClient = vrcClient;
        m_appPathsOptions = appPathsOption.Value;
        m_pathUtil = pathUtil;
    }

    public async Task Scan(CancellationToken? cancellationToken)
    {
        if (IsScanWorking)
        {
            Log.Debug($"[Scan] Already Scanning.");
            return;
        }

        IsScanWorking = true;
        Log.Info($"[Scan] Start Scanning.");
        Stopwatch sw = new Stopwatch();
        sw.Start();

        DirectoryInfo currentDir = new DirectoryInfo(m_appPathsOptions.ScanFolderPath);
        foreach (DirectoryInfo worldFolder in currentDir.EnumerateDirectories())
        {
            Log.Debug($"[Scan] worldFolder: {worldFolder}");

            cancellationToken?.ThrowIfCancellationRequested();

            string worldId = worldFolder.Name;

            DateTime modifiedAt = worldFolder.LastWriteTimeUtc;
            DateTime createdAt = worldFolder.CreationTimeUtc != modifiedAt ? worldFolder.CreationTimeUtc : DateTime.UtcNow; // BirthTime이 없으면 현재 시각으로 생성

            // Process WorldMetadata
            Log.Debug($"[Scan] HasWorld: {worldId} = {m_database.HasWorld(worldId)}");

            if (m_database.HasWorld(worldId) == false)
            {
                Log.Debug($"[Scan] AddWorldData: {worldId}, {createdAt}");
                m_database.AddWorldData(worldId, createdAt);
            }

            Log.Debug($"[Scan] IsWorldMetadataNeedToUpdate: {worldId}, {m_database.IsWorldMetadataNeedToUpdate(worldId)}");
            if (m_database.IsWorldMetadataNeedToUpdate(worldId))
            {
                Log.Debug($"[Scan] Lets go Fetch {worldId}");
                WorldMetadata? worldMetadata = await m_vrcClient.FetchVRCWorldMetadata(worldId);
                Log.Debug($"[Scan] Fetched Data: {worldId}, {worldMetadata}");
                if (worldMetadata != null)
                    m_database.UpdateWorldMetaData(worldId, worldMetadata);
            }

            // Process Image
            if (modifiedAt != m_database.GetLastFolderModifiedTime(worldId))
            {
                List<string> storedImagePathList = m_database.GetWorldImagePathList(worldId);
                List<string> existImagePathList = GetImagePathListInDirectory(worldFolder);

                // 새로운 이미지에 대한 처리
                List<string> addedImagePathList = existImagePathList.Except(storedImagePathList).ToList();
                foreach (string addedImagePath in addedImagePathList)
                {
                    m_imageJobChannel.Writer.TryWrite(new Channels.ImageJob(worldId, addedImagePath));
                }

                // 삭제된 이미지에 대한 처리
                List<string> removedImagePathList = storedImagePathList.Except(existImagePathList).ToList();
                foreach (string removedImagePath in removedImagePathList)
                {
                    // 삭제된 이미지 database에서 제거
                    m_database.RemoveWorldImage(worldId, removedImagePath);

                    // 파일 삭제
                    string thumbPath = m_pathUtil.GetThumbPath(worldId, removedImagePath);
                    if (File.Exists(thumbPath))
                        File.Delete(thumbPath);

                    string viewPath = m_pathUtil.GetViewPath(worldId, removedImagePath);
                    if (File.Exists(viewPath))
                        File.Delete(viewPath);
                }

                // LastModifiedTime 갱신
                // added는 사진 추가 작업이 보장되지 않으므로 함부로 갱신하면 안 된다.
                // 한바퀴 더 돌아서 완전한 데이터인게 확인되면(= added없음) 그 땐 갱신해도 좋음.
                // 단순히 removed만 발생한 경우는 데이터 수정만 있으니 문제 없음.
                // 우려되는 시나리오:
                //      added가 5 있는데 잘 되겠지 하고 modified 갱신
                //      문제 발생해서 강종됨
                //      이미지 처리할게 남아있지만 modifiedAt 비교에서 prune됨 
                if (addedImagePathList.Count == 0)
                    m_database.UpdateLastFolderModifiedTime(worldId, modifiedAt);
            }
        }

        sw.Stop();
        IsScanWorking = false;
        Log.Info($"[Scan] End Scanning. Elapsed Time: {sw.ElapsedMilliseconds}ms");
    }

    private List<string> GetImagePathListInDirectory(DirectoryInfo dir)
    {
        List<string> result = new();
        foreach (FileInfo imageFileInfo in dir.EnumerateFiles("*.png;*.jpg;*.jpeg;*.jfif;*.webp;*.bmp", SearchOption.TopDirectoryOnly))
        {
            result.Add(m_pathUtil.ToRelativePath(imageFileInfo.FullName));
        }
        return result;
    }
}