using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Windows.Graphics.Imaging;

namespace ImageViewer.App.Services;

public sealed class WindowsImageProcessingService : IExportService, ITransformService
{
    public async Task<Result> ExportImageAsync(
        string sourceFilePath,
        string destinationFilePath,
        ExportOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(sourceFilePath))
            {
                return Result.Failure(ViewerError.ExportError("ExportImage", sourceFilePath));
            }

            if (File.Exists(destinationFilePath) && !options.OverwriteExisting)
            {
                return Result.Failure(ViewerError.ExportError("ExportImage", destinationFilePath));
            }

            var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            if (options.PreserveExif && options.TargetFormat is ImageExportFormat.Jpeg or ImageExportFormat.Png)
            {
                await ExportWithImageSharpAsync(sourceFilePath, destinationFilePath, options, cancellationToken)
                    .ConfigureAwait(false);
                return Result.Success();
            }

            if (options.TargetFormat == ImageExportFormat.Webp)
            {
                if (string.Equals(Path.GetExtension(sourceFilePath), ".webp", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
                    return Result.Success();
                }

                var webpBitmap = await DecodeBitmapAsync(sourceFilePath, cancellationToken).ConfigureAwait(false);
                await EncodeWebpAsync(destinationFilePath, webpBitmap, options, cancellationToken).ConfigureAwait(false);
                return Result.Success();
            }

            var bitmap = await DecodeBitmapAsync(sourceFilePath, cancellationToken).ConfigureAwait(false);
            await EncodeBitmapAsync(
                    destinationFilePath,
                    GetEncoderId(options.TargetFormat),
                    bitmap.Pixels,
                    bitmap.Width,
                    bitmap.Height,
                    cancellationToken)
                .ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OperationCanceledException)
        {
            return Result.Failure(ViewerError.ExportError("ExportImage", destinationFilePath, ex));
        }
    }

    public Task<Result<TransformedImage>> ApplyTransformAsync(
        DecodedImage sourceImage,
        IReadOnlyList<TransformOperation> operations,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var width = sourceImage.Metadata.PixelWidth;
            var height = sourceImage.Metadata.PixelHeight;

            foreach (var operation in operations)
            {
                if (operation is TransformOperation.RotateLeft or TransformOperation.RotateRight)
                {
                    (width, height) = (height, width);
                }
            }

            return Task.FromResult(Result<TransformedImage>.Success(new TransformedImage(
                sourceImage,
                sourceImage.Metadata with { PixelWidth = width, PixelHeight = height },
                operations.ToArray())));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<TransformedImage>.Failure(
                ViewerError.TransformError("ApplyTransform", sourceImage.FilePath, ex)));
        }
    }

    public async Task<Result> SaveTransformsAsync(
        string filePath,
        IReadOnlyList<TransformOperation> operations,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return Result.Failure(ViewerError.TransformSaveError("SaveTransforms", filePath));
        }

        if (operations.Count == 0)
        {
            return Result.Success();
        }

        var tempPath = Path.Combine(
            Path.GetDirectoryName(filePath) ?? Path.GetTempPath(),
            $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bitmap = await DecodeBitmapAsync(filePath, cancellationToken).ConfigureAwait(false);
            foreach (var operation in operations)
            {
                bitmap = ApplyPixelTransform(bitmap, operation);
            }

            await EncodeBitmapAsync(
                    tempPath,
                    GetEncoderIdFromPath(filePath),
                    bitmap.Pixels,
                    bitmap.Width,
                    bitmap.Height,
                    cancellationToken)
                .ConfigureAwait(false);

            File.Copy(tempPath, filePath, overwrite: true);
            File.Delete(tempPath);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OperationCanceledException)
        {
            TryDelete(tempPath);
            return Result.Failure(ViewerError.TransformSaveError("SaveTransforms", filePath, ex));
        }
    }

    private static async Task<BitmapBuffer> DecodeBitmapAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var randomAccessStream = source.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        cancellationToken.ThrowIfCancellationRequested();

        var pixelData = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            new BitmapTransform(),
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.DoNotColorManage);

        return new BitmapBuffer(
            pixelData.DetachPixelData(),
            checked((int)decoder.PixelWidth),
            checked((int)decoder.PixelHeight));
    }

    private static async Task EncodeBitmapAsync(
        string filePath,
        Guid encoderId,
        byte[] pixels,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        await using var destination = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var randomAccessStream = destination.AsRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(encoderId, randomAccessStream);
        cancellationToken.ThrowIfCancellationRequested();

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            checked((uint)width),
            checked((uint)height),
            96,
            96,
            pixels);

        await encoder.FlushAsync();
    }

    private static async Task ExportWithImageSharpAsync(
        string sourceFilePath,
        string destinationFilePath,
        ExportOptions options,
        CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(sourceFilePath, cancellationToken).ConfigureAwait(false);
        switch (options.TargetFormat)
        {
            case ImageExportFormat.Jpeg:
                await image.SaveAsync(
                        destinationFilePath,
                        new JpegEncoder { Quality = options.JpegQuality },
                        cancellationToken)
                    .ConfigureAwait(false);
                break;

            case ImageExportFormat.Png:
                await image.SaveAsync(
                        destinationFilePath,
                        new PngEncoder(),
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
        }
    }

    private static async Task EncodeWebpAsync(
        string filePath,
        BitmapBuffer bitmap,
        ExportOptions options,
        CancellationToken cancellationToken)
    {
        using var image = Image.LoadPixelData<Bgra32>(bitmap.Pixels, bitmap.Width, bitmap.Height);
        await image.SaveAsync(
                filePath,
                new WebpEncoder { Quality = options.JpegQuality },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static BitmapBuffer ApplyPixelTransform(BitmapBuffer bitmap, TransformOperation operation)
    {
        return operation switch
        {
            TransformOperation.RotateLeft => RotateLeft(bitmap),
            TransformOperation.RotateRight => RotateRight(bitmap),
            TransformOperation.FlipHorizontal => FlipHorizontal(bitmap),
            TransformOperation.FlipVertical => FlipVertical(bitmap),
            _ => bitmap
        };
    }

    private static BitmapBuffer RotateRight(BitmapBuffer source)
    {
        var destination = new byte[source.Pixels.Length];
        var destinationWidth = source.Height;
        var destinationHeight = source.Width;

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                CopyPixel(
                    source.Pixels,
                    destination,
                    SourceOffset(source.Width, x, y),
                    SourceOffset(destinationWidth, source.Height - 1 - y, x));
            }
        }

        return new BitmapBuffer(destination, destinationWidth, destinationHeight);
    }

    private static BitmapBuffer RotateLeft(BitmapBuffer source)
    {
        var destination = new byte[source.Pixels.Length];
        var destinationWidth = source.Height;
        var destinationHeight = source.Width;

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                CopyPixel(
                    source.Pixels,
                    destination,
                    SourceOffset(source.Width, x, y),
                    SourceOffset(destinationWidth, y, source.Width - 1 - x));
            }
        }

        return new BitmapBuffer(destination, destinationWidth, destinationHeight);
    }

    private static BitmapBuffer FlipHorizontal(BitmapBuffer source)
    {
        var destination = new byte[source.Pixels.Length];

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                CopyPixel(
                    source.Pixels,
                    destination,
                    SourceOffset(source.Width, x, y),
                    SourceOffset(source.Width, source.Width - 1 - x, y));
            }
        }

        return new BitmapBuffer(destination, source.Width, source.Height);
    }

    private static BitmapBuffer FlipVertical(BitmapBuffer source)
    {
        var destination = new byte[source.Pixels.Length];

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                CopyPixel(
                    source.Pixels,
                    destination,
                    SourceOffset(source.Width, x, y),
                    SourceOffset(source.Width, x, source.Height - 1 - y));
            }
        }

        return new BitmapBuffer(destination, source.Width, source.Height);
    }

    private static int SourceOffset(int width, int x, int y)
    {
        return checked((y * width + x) * 4);
    }

    private static void CopyPixel(byte[] source, byte[] destination, int sourceOffset, int destinationOffset)
    {
        destination[destinationOffset] = source[sourceOffset];
        destination[destinationOffset + 1] = source[sourceOffset + 1];
        destination[destinationOffset + 2] = source[sourceOffset + 2];
        destination[destinationOffset + 3] = source[sourceOffset + 3];
    }

    private static Guid GetEncoderId(ImageExportFormat format)
    {
        return format switch
        {
            ImageExportFormat.Jpeg => BitmapEncoder.JpegEncoderId,
            ImageExportFormat.Bmp => BitmapEncoder.BmpEncoderId,
            ImageExportFormat.Webp => throw new NotSupportedException("WEBP encoding is handled by ImageSharp."),
            _ => BitmapEncoder.PngEncoderId
        };
    }

    private static Guid GetEncoderIdFromPath(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => BitmapEncoder.JpegEncoderId,
            ".bmp" => BitmapEncoder.BmpEncoderId,
            ".webp" => throw new NotSupportedException("WEBP encoding is not available through Windows.Graphics.Imaging."),
            ".gif" => BitmapEncoder.GifEncoderId,
            ".tif" or ".tiff" => BitmapEncoder.TiffEncoderId,
            _ => BitmapEncoder.PngEncoderId
        };
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
        }
    }

    private sealed record BitmapBuffer(byte[] Pixels, int Width, int Height);
}
