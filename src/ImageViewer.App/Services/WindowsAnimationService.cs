using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace ImageViewer.App.Services;

public sealed class WindowsAnimationService : IAnimationService
{
    private const int GifDelayMultiplierMilliseconds = 10;
    private static readonly string[] DelayPropertyNames = ["/grctlext/Delay"];

    public async Task<Result<AnimatedGif>> LoadAnimatedGifAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return Result<AnimatedGif>.Failure(ViewerError.AnimationError("LoadAnimatedGif", filePath));
        }

        try
        {
            await using var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = source.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            var frames = new List<AnimationFrame>(checked((int)decoder.FrameCount));

            for (uint index = 0; index < decoder.FrameCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = await decoder.GetFrameAsync(index);
                var delay = await ExtractFrameDelayAsync(frame);
                var encodedBytes = await EncodeFrameAsPngAsync(frame);

                frames.Add(new AnimationFrame(
                    encodedBytes,
                    checked((int)frame.PixelWidth),
                    checked((int)frame.PixelHeight),
                    delay));
            }

            return Result<AnimatedGif>.Success(new AnimatedGif(filePath, frames));
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or OperationCanceledException
            or ArgumentException
            or InvalidOperationException
            or NotSupportedException)
        {
            return Result<AnimatedGif>.Failure(ViewerError.AnimationError("LoadAnimatedGif", filePath, ex));
        }
    }

    private static async Task<TimeSpan> ExtractFrameDelayAsync(BitmapFrame frame)
    {
        try
        {
            var properties = await frame.BitmapProperties.GetPropertiesAsync(DelayPropertyNames);
            if (!properties.TryGetValue(DelayPropertyNames[0], out var property) || property.Value is null)
            {
                return TimeSpan.Zero;
            }

            var gifDelay = Convert.ToInt32(property.Value, System.Globalization.CultureInfo.InvariantCulture);
            return TimeSpan.FromMilliseconds(gifDelay * GifDelayMultiplierMilliseconds);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private static async Task<byte[]> EncodeFrameAsPngAsync(BitmapFrame frame)
    {
        var pixelData = await frame.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

        using var memory = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memory);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            frame.PixelWidth,
            frame.PixelHeight,
            96,
            96,
            pixelData.DetachPixelData());
        await encoder.FlushAsync();

        var encodedBytes = new byte[memory.Size];
        memory.Seek(0);
        await memory.ReadAsync(encodedBytes.AsBuffer(), checked((uint)encodedBytes.Length), InputStreamOptions.None);
        return encodedBytes;
    }
}
