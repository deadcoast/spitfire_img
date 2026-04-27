using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class ThumbnailService : IThumbnailService
{
    public const int ThumbnailMaxDimension = 120;
    public const long DefaultMaxCacheBytes = 100L * 1024 * 1024;

    private readonly IImageDecodeService _imageDecodeService;
    private readonly long _maxCacheBytes;
    private readonly Dictionary<string, LinkedListNode<ThumbnailCacheEntry>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<ThumbnailCacheEntry> _lru = new();
    private long _cacheSizeBytes;

    public ThumbnailService(IImageDecodeService imageDecodeService, long maxCacheBytes = DefaultMaxCacheBytes)
    {
        _imageDecodeService = imageDecodeService;
        _maxCacheBytes = Math.Max(0, maxCacheBytes);
    }

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

        var decodeResult = await _imageDecodeService.DecodeImageAsync(item.FilePath, cancellationToken)
            .ConfigureAwait(false);
        if (decodeResult.IsFailure)
        {
            return Result<Thumbnail>.Failure(ViewerError.ThumbnailGenerationError("GenerateThumbnail", item.FilePath));
        }

        var (width, height) = CalculateThumbnailSize(
            decodeResult.Value.Metadata.PixelWidth,
            decodeResult.Value.Metadata.PixelHeight);
        var estimatedSizeBytes = Math.Max(1, width * height * 4L);
        var thumbnail = new Thumbnail(
            item,
            EncodedBytes: [],
            width,
            height,
            estimatedSizeBytes,
            DateTimeOffset.UtcNow);

        AddToCache(thumbnail);
        return Result<Thumbnail>.Success(thumbnail);
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

    public static (int Width, int Height) CalculateThumbnailSize(int sourceWidth, int sourceHeight)
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
        while (_cacheSizeBytes > _maxCacheBytes && _lru.Last is not null)
        {
            var last = _lru.Last;
            _lru.RemoveLast();
            _cache.Remove(last.Value.FilePath);
            _cacheSizeBytes -= last.Value.Thumbnail.SizeBytes;
        }
    }

    private sealed record ThumbnailCacheEntry(string FilePath, Thumbnail Thumbnail);
}
