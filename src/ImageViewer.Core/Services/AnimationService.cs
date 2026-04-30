using System.Buffers.Binary;
using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed class AnimationService : IAnimationService
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
            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            var animation = await Task.Run(
                    () => ParseGif(filePath, bytes),
                    cancellationToken)
                .ConfigureAwait(false);

            return Result<AnimatedGif>.Success(animation);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return Result<AnimatedGif>.Failure(ViewerError.AnimationError("LoadAnimatedGif", filePath, ex));
        }
    }

    public async Task<Result<bool>> IsAnimatedGifAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var result = await LoadAnimatedGifAsync(filePath, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Result<bool>.Success(result.Value.IsAnimated)
            : Result<bool>.Failure(result.Error);
    }

    private static AnimatedGif ParseGif(string filePath, byte[] bytes)
    {
        if (bytes.Length < 14 || (!IsAscii(bytes, 0, "GIF87a") && !IsAscii(bytes, 0, "GIF89a")))
        {
            throw new InvalidDataException("Invalid GIF header.");
        }

        var width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2));
        var height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2));
        var offset = 13;
        if ((bytes[10] & 0x80) != 0)
        {
            offset += GetColorTableLength(bytes[10]);
        }

        EnsureAvailable(bytes, offset, 0);
        var frames = new List<AnimationFrame>();
        var nextFrameDelay = DefaultFrameDelay;

        while (offset < bytes.Length)
        {
            var blockType = bytes[offset++];
            if (blockType == 0x3B)
            {
                break;
            }

            if (blockType == 0x21)
            {
                nextFrameDelay = ReadExtension(bytes, ref offset, nextFrameDelay);
                continue;
            }

            if (blockType != 0x2C)
            {
                throw new InvalidDataException($"Unsupported GIF block type 0x{blockType:X2}.");
            }

            var frame = ReadImageDescriptor(bytes, ref offset, frames.Count, nextFrameDelay);
            frames.Add(frame);
            nextFrameDelay = DefaultFrameDelay;
        }

        if (frames.Count == 0)
        {
            throw new InvalidDataException("GIF contains no image frames.");
        }

        var metadata = new ImageMetadata(width, height, bytes.Length);
        return new AnimatedGif(filePath, metadata, frames);
    }

    private static TimeSpan ReadExtension(byte[] bytes, ref int offset, TimeSpan currentDelay)
    {
        EnsureAvailable(bytes, offset, 1);
        var label = bytes[offset++];
        if (label != 0xF9)
        {
            SkipSubBlocks(bytes, ref offset);
            return currentDelay;
        }

        EnsureAvailable(bytes, offset, 6);
        var blockSize = bytes[offset++];
        if (blockSize != 4)
        {
            throw new InvalidDataException("Invalid GIF graphics control extension.");
        }

        offset++;
        var delayCentiseconds = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
        offset += 2;
        offset++;

        if (bytes[offset++] != 0x00)
        {
            throw new InvalidDataException("GIF graphics control extension was not terminated.");
        }

        return NormalizeDelay(delayCentiseconds);
    }

    private static AnimationFrame ReadImageDescriptor(
        byte[] bytes,
        ref int offset,
        int frameIndex,
        TimeSpan delay)
    {
        var frameStart = offset - 1;
        EnsureAvailable(bytes, offset, 9);

        offset += 4;
        var frameWidth = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
        offset += 2;
        var frameHeight = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
        offset += 2;
        var packedFields = bytes[offset++];

        if ((packedFields & 0x80) != 0)
        {
            offset += GetColorTableLength(packedFields);
        }

        EnsureAvailable(bytes, offset, 1);
        offset++;
        SkipSubBlocks(bytes, ref offset);

        var encodedBytes = bytes[frameStart..offset];
        return new AnimationFrame(frameIndex, delay, encodedBytes, frameWidth, frameHeight);
    }

    private static void SkipSubBlocks(byte[] bytes, ref int offset)
    {
        while (true)
        {
            EnsureAvailable(bytes, offset, 1);
            var blockLength = bytes[offset++];
            if (blockLength == 0)
            {
                return;
            }

            offset += blockLength;
            EnsureAvailable(bytes, offset, 0);
        }
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

    private static int GetColorTableLength(byte packedFields)
    {
        var tableSizePower = (packedFields & 0x07) + 1;
        return 3 * (1 << tableSizePower);
    }

    private static void EnsureAvailable(byte[] bytes, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > bytes.Length)
        {
            throw new InvalidDataException("GIF data ended unexpectedly.");
        }
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
