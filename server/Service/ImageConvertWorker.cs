using Microsoft.Extensions.Options;
using System.Threading.Channels;
using server.Core;
using server.Service;
using server.Util;
using SkiaSharp;
using System.Diagnostics;

public class ImageConvertWorker : BackgroundService
{
    private readonly Channel<Channels.ImageJob> m_channel;
    private readonly ImageOptions m_imageOptions;
    private readonly IPathUtil m_pathUtil;
    private readonly IServiceScopeFactory m_scopeFactory;

    public ImageConvertWorker(
        Channel<Channels.ImageJob> channel, 
        IServiceScopeFactory scopeFactory,
        IOptions<ImageOptions> imageOptions,
        IPathUtil pathUtil)
    {
        m_channel = channel;
        m_scopeFactory = scopeFactory;
        m_imageOptions = imageOptions.Value;
        m_pathUtil = pathUtil;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = m_channel.Reader;

        while (await reader.WaitToReadAsync(stoppingToken))
        {
            while (reader.TryRead(out Channels.ImageJob? job))
            {
                try
                {
                    Stopwatch sw = new();
                    sw.Start();
                    await ConvertToWebpAsync(job, stoppingToken);
                    sw.Stop();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    Log.Info("Cancellation requested");
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed converting {job.worldId} / {job.SourcePath}: {ex}");
                }
            }
        }
    }

    private async Task ConvertToWebpAsync(Channels.ImageJob job, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        byte[] input = await File.ReadAllBytesAsync(job.SourcePath, ct);
        SKData? skData = SKData.CreateCopy(input);
        using var codec = SKCodec.Create(skData) ?? throw new InvalidOperationException("Invalid image");

        using var bitmap = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("Decode failed");
        using var image = SKImage.FromBitmap(bitmap);

        int width = codec.Info.Width;
        int height = codec.Info.Height;

        // Thumb
        string thumbPath = m_pathUtil.GetThumbPath(job.worldId, job.SourcePath);
        if (File.Exists(thumbPath) == false)
        {
            var webpQuality = Math.Clamp(m_imageOptions.ThumbQuality, 1, 100);
            using var data = image.Encode(SKEncodedImageFormat.Webp, webpQuality);
            if (data == null) throw new InvalidOperationException("WebP encode failed (Thumb)");

            Directory.CreateDirectory(Path.GetDirectoryName(thumbPath)!);
            await File.WriteAllBytesAsync(thumbPath, data.ToArray(), ct);
        }

        // View
        string viewPath = m_pathUtil.GetViewPath(job.worldId, job.SourcePath);
        if (File.Exists(viewPath) == false)
        {
            var webpQuality = Math.Clamp(m_imageOptions.ViewQuality, 1, 100);
            using var data = image.Encode(SKEncodedImageFormat.Webp, webpQuality);
            if (data == null) throw new InvalidOperationException("WebP encode failed (View)");

            Directory.CreateDirectory(Path.GetDirectoryName(viewPath)!);
            await File.WriteAllBytesAsync(viewPath, data.ToArray(), ct);
        }

        // Update WorldData
        using var scope = m_scopeFactory.CreateScope();
        Database database = scope.ServiceProvider.GetRequiredService<Database>();
        await database.AddWorldImage(job.worldId, job.SourcePath, width, height);
    }
}