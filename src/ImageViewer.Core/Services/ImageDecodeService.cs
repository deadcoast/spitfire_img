using System.Buffers.Binary;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class ImageDecodeService : IImageDecodeService
{
    private readonly ICodecDetectionService? _codecDetectionService;
    private readonly IPlatformImageDecodeService? _platformImageDecodeService;

    public ImageDecodeService()
    {
    }

    public ImageDecodeService(
        ICodecDetectionService codecDetectionService,
        IPlatformImageDecodeService platformImageDecodeService)
    {
        _codecDetectionService = codecDetectionService;
        _platformImageDecodeService = platformImageDecodeService;
    }

    public async Task<Result<DecodedImage>> DecodeImageAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath);
        if (ImageFormatCatalog.IsHeicFormat(extension))
        {
            return await DecodeHeicImageAsync(filePath, cancellationToken).ConfigureAwait(false);
        }

        if (ImageFormatCatalog.IsAvifFormat(extension))
        {
            return await DecodeAvifImageAsync(filePath, cancellationToken).ConfigureAwait(false);
        }

        if (!IsSupportedFormat(extension))
        {
            return Result<DecodedImage>.Failure(ViewerError.UnsupportedFormatError("DecodeImage", filePath));
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            var (width, height) = await Task.Run(() => ReadDimensions(extension, bytes), cancellationToken)
                .ConfigureAwait(false);
            var metadata = new ImageMetadata(width, height, new FileInfo(filePath).Length);
            return Result<DecodedImage>.Success(new DecodedImage(filePath, metadata));
        }
        catch (OutOfMemoryException ex)
        {
            return Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath, ex, isMemoryError: true));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath, ex));
        }
    }

    public bool IsSupportedFormat(string extension)
    {
        return ImageFormatCatalog.IsSupportedFormat(extension) ||
               (ImageFormatCatalog.IsHeicFormat(extension) && IsHeicCodecSupported()) ||
               (ImageFormatCatalog.IsAvifFormat(extension) && IsAvifCodecSupported());
    }

    public bool IsDeferredFormat(string extension)
    {
        return ImageFormatCatalog.IsDeferredFormat(extension);
    }

    public async Task<Result<int>> GetFrameCountAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var extension = Path.GetExtension(filePath);
            if (!IsSupportedFormat(extension))
            {
                return Result<int>.Failure(ViewerError.UnsupportedFormatError("GetFrameCount", filePath));
            }

            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            var frameCount = extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                ? CountGifFrames(bytes)
                : 1;

            return Result<int>.Success(Math.Max(1, frameCount));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return Result<int>.Failure(ViewerError.DecodeError("GetFrameCount", filePath, ex));
        }
    }

    private static (int Width, int Height) ReadDimensions(string extension, byte[] bytes)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => ReadPngDimensions(bytes),
            ".jpg" or ".jpeg" => ReadJpegDimensions(bytes),
            ".bmp" => ReadBmpDimensions(bytes),
            ".webp" => ReadWebpDimensions(bytes),
            ".gif" => ReadGifDimensions(bytes),
            ".tiff" or ".tif" => ReadTiffDimensions(bytes),
            _ => throw new InvalidDataException("Unsupported image format.")
        };
    }

    private async Task<Result<DecodedImage>> DecodeHeicImageAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!IsHeicCodecSupported())
        {
            return Result<DecodedImage>.Failure(ViewerError.CodecUnavailableError(
                "DecodeImage",
                "HEIC",
                "HEIF Image Extensions",
                filePath));
        }

        if (_platformImageDecodeService is null)
        {
            return Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath));
        }

        return await _platformImageDecodeService.DecodeImageAsync(filePath, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result<DecodedImage>> DecodeAvifImageAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!IsAvifCodecSupported())
        {
            return Result<DecodedImage>.Failure(ViewerError.CodecUnavailableError(
                "DecodeImage",
                "AVIF",
                "AV1 Video Extension",
                filePath));
        }

        if (_platformImageDecodeService is null)
        {
            return Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath));
        }

        return await _platformImageDecodeService.DecodeImageAsync(filePath, cancellationToken)
            .ConfigureAwait(false);
    }

    private bool IsHeicCodecSupported()
    {
        var result = _codecDetectionService?.IsHeicSupported();
        return result?.IsSuccess == true && result.Value;
    }

    private bool IsAvifCodecSupported()
    {
        var result = _codecDetectionService?.IsAvifSupported();
        return result?.IsSuccess == true && result.Value;
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < 24 || !bytes.AsSpan(0, 8).SequenceEqual(signature) ||
            bytes[12] != 0x49 || bytes[13] != 0x48 || bytes[14] != 0x44 || bytes[15] != 0x52)
        {
            throw new InvalidDataException("Invalid PNG header.");
        }

        return (
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static (int Width, int Height) ReadBmpDimensions(byte[] bytes)
    {
        if (bytes.Length < 26 || bytes[0] != 0x42 || bytes[1] != 0x4D)
        {
            throw new InvalidDataException("Invalid BMP header.");
        }

        var width = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(18, 4));
        var height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(22, 4)));
        return (width, height);
    }

    private static (int Width, int Height) ReadWebpDimensions(byte[] bytes)
    {
        if (bytes.Length < 30 || !IsAscii(bytes, 0, "RIFF") || !IsAscii(bytes, 8, "WEBP"))
        {
            throw new InvalidDataException("Invalid WEBP header.");
        }

        if (IsAscii(bytes, 12, "VP8X"))
        {
            var width = 1 + ReadUInt24LittleEndian(bytes.AsSpan(24, 3));
            var height = 1 + ReadUInt24LittleEndian(bytes.AsSpan(27, 3));
            return (width, height);
        }

        if (IsAscii(bytes, 12, "VP8 "))
        {
            if (bytes.Length < 30 || bytes[23] != 0x9D || bytes[24] != 0x01 || bytes[25] != 0x2A)
            {
                throw new InvalidDataException("Invalid WEBP VP8 header.");
            }

            var width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26, 2)) & 0x3FFF;
            var height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28, 2)) & 0x3FFF;
            return (width, height);
        }

        throw new InvalidDataException("Unsupported WEBP header variant.");
    }

    private static (int Width, int Height) ReadGifDimensions(byte[] bytes)
    {
        if (bytes.Length < 10 || (!IsAscii(bytes, 0, "GIF87a") && !IsAscii(bytes, 0, "GIF89a")))
        {
            throw new InvalidDataException("Invalid GIF header.");
        }

        return (
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2)));
    }

    private static (int Width, int Height) ReadTiffDimensions(byte[] bytes)
    {
        if (bytes.Length < 8)
        {
            throw new InvalidDataException("Invalid TIFF header.");
        }

        var littleEndian = bytes[0] == 0x49 && bytes[1] == 0x49;
        var bigEndian = bytes[0] == 0x4D && bytes[1] == 0x4D;
        if (!littleEndian && !bigEndian)
        {
            throw new InvalidDataException("Invalid TIFF byte order.");
        }

        if (ReadUInt16(bytes.AsSpan(2, 2), littleEndian) != 42)
        {
            throw new InvalidDataException("Invalid TIFF magic number.");
        }

        var ifdOffset = ReadInt32(bytes.AsSpan(4, 4), littleEndian);
        if (ifdOffset < 0 || ifdOffset + 2 > bytes.Length)
        {
            throw new InvalidDataException("Invalid TIFF IFD offset.");
        }

        var entryCount = ReadUInt16(bytes.AsSpan(ifdOffset, 2), littleEndian);
        var offset = ifdOffset + 2;
        int? width = null;
        int? height = null;

        for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (offset + 12 > bytes.Length)
            {
                break;
            }

            var tag = ReadUInt16(bytes.AsSpan(offset, 2), littleEndian);
            var type = ReadUInt16(bytes.AsSpan(offset + 2, 2), littleEndian);
            var value = type == 3
                ? ReadUInt16(bytes.AsSpan(offset + 8, 2), littleEndian)
                : ReadInt32(bytes.AsSpan(offset + 8, 4), littleEndian);

            if (tag == 256)
            {
                width = value;
            }
            else if (tag == 257)
            {
                height = value;
            }

            offset += 12;
        }

        return width.HasValue && height.HasValue
            ? (width.Value, height.Value)
            : throw new InvalidDataException("TIFF dimensions were not found.");
    }

    private static (int Width, int Height) ReadJpegDimensions(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            throw new InvalidDataException("Invalid JPEG header.");
        }

        var offset = 2;
        while (offset + 9 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var marker = bytes[offset + 1];
            offset += 2;

            if (marker is 0xD8 or 0xD9)
            {
                continue;
            }

            if (offset + 2 > bytes.Length)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length)
            {
                break;
            }

            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                var height = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 5, 2));
                return (width, height);
            }

            offset += segmentLength;
        }

        throw new InvalidDataException("JPEG dimensions were not found.");
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
    }

    private static int CountGifFrames(byte[] bytes)
    {
        ReadGifDimensions(bytes);
        return bytes.Count(value => value == 0x2C);
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> bytes, bool littleEndian)
    {
        return littleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(bytes)
            : BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    private static int ReadInt32(ReadOnlySpan<byte> bytes, bool littleEndian)
    {
        return littleEndian
            ? BinaryPrimitives.ReadInt32LittleEndian(bytes)
            : BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    private static bool IsAscii(byte[] bytes, int offset, string value)
    {
        if (offset + value.Length > bytes.Length)
        {
            return false;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (bytes[offset + index] != value[index])
            {
                return false;
            }
        }

        return true;
    }
}
