using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using Windows.Graphics.Imaging;

namespace ImageViewer.App.Services;

public sealed class WindowsImageMetadataReader : IWindowsImageMetadataReader
{
    public async Task<Result<ImageMetadata>> ReadMetadataAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = source.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            var metadata = new ImageMetadata(
                checked((int)decoder.PixelWidth),
                checked((int)decoder.PixelHeight),
                new FileInfo(filePath).Length);

            return Result<ImageMetadata>.Success(metadata);
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or OperationCanceledException
            or ArgumentException
            or InvalidDataException
            or OverflowException)
        {
            return Result<ImageMetadata>.Failure(ViewerError.DecodeError("ReadMetadata", filePath, ex));
        }
    }
}
