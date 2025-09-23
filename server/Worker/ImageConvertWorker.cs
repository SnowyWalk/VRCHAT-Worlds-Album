﻿using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using SkiaSharp;

public sealed class ImageConvertWorker : BackgroundService
{
    private readonly Channel<ImageJob> _channel;
    private readonly ILogger<ImageConvertWorker> _logger;

    public ImageConvertWorker(Channel<ImageJob> channel, ILogger<ImageConvertWorker> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImageConvertWorker started");
        var reader = _channel.Reader;

        while (await reader.WaitToReadAsync(stoppingToken))
        {
            while (reader.TryRead(out var job))
            {
                try
                {
                    await ConvertToWebpAsync(job, stoppingToken);
                    _logger.LogInformation("Converted: {src} -> {dst} (q{q})", job.SourcePath, job.DestPath, job.Quality);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancellation requested");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed converting {src}", job.SourcePath);
                }
            }
        }
    }

    private static Task ConvertToWebpAsync(ImageJob job, CancellationToken ct)
    {
        // 파일 I/O는 async, 인코딩은 CPU 바운드 → Task.Run으로 분리
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            using var input = File.OpenRead(job.SourcePath);
            using var skData = SKData.Create(input);
            using var codec = SKCodec.Create(skData) ?? throw new InvalidOperationException("Invalid image");

            using var bitmap = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("Decode failed");
            using var image = SKImage.FromBitmap(bitmap);

            var webpQuality = Math.Clamp(job.Quality, 1, 100);
            var data = image.Encode(SKEncodedImageFormat.Webp, webpQuality);
            if (data == null) throw new InvalidOperationException("WebP encode failed");

            Directory.CreateDirectory(Path.GetDirectoryName(job.DestPath)!);
            using var output = File.Open(job.DestPath, FileMode.Create, FileAccess.Write, FileShare.None);
            data.SaveTo(output);
        }, ct);
    }
}
