using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace ImageViewer.App.Services;

public sealed class WindowsThumbnailService : IThumbnailService
{
    public const int ThumbnailMaxDimension = 120;
    public const long DefaultMaxCacheBytes = 100L * 1024 * 1024;

    private readonly Dictionary<string, LinkedListNode<ThumbnailCacheEntry>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<ThumbnailCacheEntry> _lru = new();
    private long _cacheSizeBytes;

    public async Task<Result<Thumbnail>> GetThumbnailAsync(
        ImageItem item,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(item.FilePath, out var node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);
            return Result<Thumbnail>.Success(node.Value.Thumbnail with { LastAccessed = DateTimeOffset.UtcNow });
        }

        try
        {
            var thumbnail = await GenerateThumbnailAsync(item, cancellationToken).ConfigureAwait(false);
            AddToCache(thumbnail);
            return Result<Thumbnail>.Success(thumbnail);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or OperationCanceledException
            or OverflowException)
        {
            return Result<Thumbnail>.Failure(ViewerError.ThumbnailGenerationError("GenerateThumbnail", item.FilePath, ex));
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
        _lru.Clear();
        _cacheSizeBytes = 0;
    }

    public long GetCacheSize()
    {
        return _cacheSizeBytes;
    }

    private static async Task<Thumbnail> GenerateThumbnailAsync(
        ImageItem item,
        CancellationToken cancellationToken)
    {
        await using var source = File.Open(item.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sourceStream = source.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(sourceStream);
        cancellationToken.ThrowIfCancellationRequested();

        var (width, height) = CalculateThumbnailSize(
            checked((int)decoder.PixelWidth),
            checked((int)decoder.PixelHeight));

        var transform = new BitmapTransform
        {
            ScaledWidth = checked((uint)width),
            ScaledHeight = checked((uint)height),
            InterpolationMode = BitmapInterpolationMode.Fant
        };

        var pixelData = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.DoNotColorManage);

        using var memory = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memory);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            checked((uint)width),
            checked((uint)height),
            96,
            96,
            pixelData.DetachPixelData());
        await encoder.FlushAsync();

        var encodedLength = checked((int)memory.Size);
        var encodedBytes = new byte[encodedLength];
        memory.Seek(0);
        await memory.ReadAsync(encodedBytes.AsBuffer(), checked((uint)encodedLength), InputStreamOptions.None);

        return new Thumbnail(
            item,
            encodedBytes,
            width,
            height,
            encodedBytes.LongLength,
            DateTimeOffset.UtcNow);
    }

    private static (int Width, int Height) CalculateThumbnailSize(int sourceWidth, int sourceHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return (ThumbnailMaxDimension, ThumbnailMaxDimension);
        }

        var scale = ThumbnailMaxDimension / (double)Math.Max(sourceWidth, sourceHeight);
        return (
            Math.Max(1, (int)Math.Round(sourceWidth * scale)),
            Math.Max(1, (int)Math.Round(sourceHeight * scale)));
    }

    private void AddToCache(Thumbnail thumbnail)
    {
        var entry = new ThumbnailCacheEntry(thumbnail.Item.FilePath, thumbnail);
        var node = new LinkedListNode<ThumbnailCacheEntry>(entry);
        _lru.AddFirst(node);
        _cache[thumbnail.Item.FilePath] = node;
        _cacheSizeBytes += thumbnail.SizeBytes;
        EvictIfNeeded();
    }

    private void EvictIfNeeded()
    {
        while (_cacheSizeBytes > DefaultMaxCacheBytes && _lru.Last is not null)
        {
            var last = _lru.Last;
            _lru.RemoveLast();
            _cache.Remove(last.Value.FilePath);
            _cacheSizeBytes -= last.Value.Thumbnail.SizeBytes;
        }
    }

    private sealed record ThumbnailCacheEntry(string FilePath, Thumbnail Thumbnail);
}
