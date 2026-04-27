using System.Buffers.Binary;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class ImageDecodeService : IImageDecodeService
{
    public async Task<Result<DecodedImage>> DecodeImageAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath);
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
        return ImageFormatCatalog.IsSupportedFormat(extension);
    }

    public bool IsDeferredFormat(string extension)
    {
        return ImageFormatCatalog.IsDeferredFormat(extension);
    }

    private static (int Width, int Height) ReadDimensions(string extension, byte[] bytes)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => ReadPngDimensions(bytes),
            ".jpg" or ".jpeg" => ReadJpegDimensions(bytes),
            ".bmp" => ReadBmpDimensions(bytes),
            ".webp" => ReadWebpDimensions(bytes),
            _ => throw new InvalidDataException("Unsupported image format.")
        };
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
