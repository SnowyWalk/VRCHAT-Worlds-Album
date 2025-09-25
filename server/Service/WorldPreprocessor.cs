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
    private readonly VRCClient m_vrcClient;
    private readonly AppPathsOptions m_appPathsOptions;
    private readonly IPathUtil m_pathUtil;
    private readonly IServiceScopeFactory m_scopeFactory;
    public bool IsScanWorking
    {
        get; private set;
    }

    public WorldPreprocessor(
        Channel<Channels.ImageJob> imageJobChannel,
        IServiceScopeFactory scopeFactory,
        VRCClient vrcClient,
        IOptions<AppPathsOptions> appPathsOption,
        IPathUtil pathUtil)
    {
        m_imageJobChannel = imageJobChannel;
        m_vrcClient = vrcClient;
        m_appPathsOptions = appPathsOption.Value;
        m_pathUtil = pathUtil;
        m_scopeFactory = scopeFactory;
    }

    public async Task Scan(CancellationToken? cancellationToken)
    {
        if (IsScanWorking)
            return;

        using IServiceScope scope = m_scopeFactory.CreateScope();
        Database database = scope.ServiceProvider.GetRequiredService<Database>();

        IsScanWorking = true;
        Log.Info($"[Scan] Start Scanning.");
        Stopwatch sw = new Stopwatch();
        sw.Start();

        DirectoryInfo currentDir = new DirectoryInfo(m_appPathsOptions.ScanFolderPath);
        foreach (DirectoryInfo worldFolder in currentDir.EnumerateDirectories())
        {
            cancellationToken?.ThrowIfCancellationRequested();

            string worldId = worldFolder.Name;

            DateTime modifiedAt = worldFolder.LastWriteTimeUtc;
            DateTime createdAt = worldFolder.CreationTimeUtc != modifiedAt ? worldFolder.CreationTimeUtc : DateTime.UtcNow; // BirthTime이 없으면 현재 시각으로 생성

            // Process WorldMetadata
            if (await database.HasWorldData(worldId) == false)
                await database.AddWorldData(worldId, createdAt);

            if (await database.IsWorldMetadataNeedToUpdate(worldId))
            {
                WorldMetadata? worldMetadata = await m_vrcClient.FetchVRCWorldMetadata(worldId);
                if (worldMetadata != null)
                    await database.UpdateWorldMetaData(worldId, worldMetadata);
            }

            // Process Image
            if (modifiedAt != await database.GetLastFolderModifiedTime(worldId))
            {
                List<string> storedImageFilenameList = await database.GetWorldImageFileNameList(worldId);
                List<string> existImageFilenameList = GetImageFilenameListInDirectory(worldFolder);

                // 새로운 이미지에 대한 처리
                List<string> addedImageFilenameList = existImageFilenameList.Except(storedImageFilenameList).ToList();
                foreach (string addedImageFilename in addedImageFilenameList)
                {
                    m_imageJobChannel.Writer.TryWrite(new Channels.ImageJob(worldId, addedImageFilename));
                }

                // 삭제된 이미지에 대한 처리
                List<string> removedImageFilenameList = storedImageFilenameList.Except(existImageFilenameList).ToList();
                foreach (string removedImageFilename in removedImageFilenameList)
                {
                    // 삭제된 이미지 database에서 제거
                    await database.RemoveWorldImage(worldId, removedImageFilename);

                    // 파일 삭제
                    string thumbPath = m_pathUtil.GetThumbImagePath(worldId, removedImageFilename);
                    if (File.Exists(thumbPath))
                        File.Delete(thumbPath);

                    string viewPath = m_pathUtil.GetViewImagePath(worldId, removedImageFilename);
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
                if (addedImageFilenameList.Count == 0)
                    await database.UpdateLastFolderModifiedTime(worldId, modifiedAt);
            }
        }

        sw.Stop();
        IsScanWorking = false;
        Log.Info($"[Scan] End Scanning. Elapsed Time: {sw.ElapsedMilliseconds}ms");
    }

    private List<string> GetImageFilenameListInDirectory(DirectoryInfo dir)
    {
        List<string> result = new();
        foreach (FileInfo imageFileInfo in dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .Where(f => new[] { ".png", ".jpg", ".jpeg", ".jfif", ".webp", ".bmp" }
                .Contains(f.Extension.ToLower())))
        {
            result.Add(imageFileInfo.Name);
        }
        return result;
    }
}