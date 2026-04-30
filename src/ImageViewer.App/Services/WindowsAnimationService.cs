using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Windows.Graphics.Imaging;

namespace ImageViewer.App.Services;

public sealed class WindowsAnimationService : IAnimationService
{
    private static readonly TimeSpan DefaultFrameDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MinimumFrameDelay = TimeSpan.FromMilliseconds(10);

    public async Task<Result<AnimatedGif>> LoadAnimatedGifAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            return Result<AnimatedGif>.Failure(ViewerError.UnsupportedFormatError("LoadAnimatedGif", filePath));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = source.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            var frames = new List<AnimationFrame>((int)decoder.FrameCount);

            for (uint frameIndex = 0; frameIndex < decoder.FrameCount; frameIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = await decoder.GetFrameAsync(frameIndex);
                var delay = await ExtractFrameDelayAsync(frame);
                var pixels = await ExtractFramePixelsAsync(frame);
                frames.Add(new AnimationFrame(
                    (int)frameIndex,
                    delay,
                    pixels,
                    (int)frame.PixelWidth,
                    (int)frame.PixelHeight));
            }

            if (frames.Count == 0)
            {
                return Result<AnimatedGif>.Failure(ViewerError.AnimationError("LoadAnimatedGif", filePath));
            }

            var metadata = new ImageMetadata(
                (int)decoder.PixelWidth,
                (int)decoder.PixelHeight,
                new FileInfo(filePath).Length);
            return Result<AnimatedGif>.Success(new AnimatedGif(filePath, metadata, frames));
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or OperationCanceledException
            or ArgumentException
            or InvalidDataException)
        {
            return Result<AnimatedGif>.Failure(ViewerError.AnimationError("LoadAnimatedGif", filePath, ex));
        }
    }

    public async Task<Result<bool>> IsAnimatedGifAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Failure(ViewerError.UnsupportedFormatError("IsAnimatedGif", filePath));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = source.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            return Result<bool>.Success(decoder.FrameCount > 1);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or OperationCanceledException
            or ArgumentException
            or InvalidDataException)
        {
            return Result<bool>.Failure(ViewerError.AnimationError("IsAnimatedGif", filePath, ex));
        }
    }

    private static async Task<byte[]> ExtractFramePixelsAsync(BitmapFrame frame)
    {
        var pixelData = await frame.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

        return pixelData.DetachPixelData();
    }

    private static async Task<TimeSpan> ExtractFrameDelayAsync(BitmapFrame frame)
    {
        try
        {
            var properties = await frame.BitmapProperties.GetPropertiesAsync(["/grctlext/Delay"]);
            if (properties.TryGetValue("/grctlext/Delay", out var delayValue) &&
                delayValue.Value is not null)
            {
                var delayCentiseconds = Convert.ToUInt16(
                    delayValue.Value,
                    System.Globalization.CultureInfo.InvariantCulture);
                return NormalizeDelay(delayCentiseconds);
            }
        }
        catch
        {
            return DefaultFrameDelay;
        }

        return DefaultFrameDelay;
    }

    private static TimeSpan NormalizeDelay(ushort delayCentiseconds)
    {
        if (delayCentiseconds == 0)
        {
            return DefaultFrameDelay;
        }

        var delay = TimeSpan.FromMilliseconds(delayCentiseconds * 10);
        return delay < MinimumFrameDelay ? MinimumFrameDelay : delay;
    }
}
