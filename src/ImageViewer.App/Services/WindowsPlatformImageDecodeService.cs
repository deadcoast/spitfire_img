using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Windows.Graphics.Imaging;

namespace ImageViewer.App.Services;

public sealed class WindowsPlatformImageDecodeService : IPlatformImageDecodeService
{
    public async Task<Result<DecodedImage>> DecodeImageAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = source.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = new ImageMetadata(
                checked((int)decoder.OrientedPixelWidth),
                checked((int)decoder.OrientedPixelHeight),
                new FileInfo(filePath).Length);
            return Result<DecodedImage>.Success(new DecodedImage(filePath, metadata));
        }
        catch (OutOfMemoryException ex)
        {
            return Result<DecodedImage>.Failure(ViewerError.DecodeError(
                "DecodeImage",
                filePath,
                ex,
                isMemoryError: true));
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or OperationCanceledException
            or ArgumentException
            or InvalidOperationException
            or NotSupportedException)
        {
            return Result<DecodedImage>.Failure(ViewerError.DecodeError("DecodeImage", filePath, ex));
        }
    }
}
