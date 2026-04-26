namespace ImageViewer.Core.Models;

public sealed record ImageMetadata(int PixelWidth, int PixelHeight, long FileSizeBytes)
{
    public string FormattedDimensions => $"{PixelWidth} × {PixelHeight}";

    public string FormattedFileSize
    {
        get
        {
            if (FileSizeBytes < 1024)
            {
                return $"{FileSizeBytes} B";
            }

            var kilobytes = FileSizeBytes / 1024d;
            if (kilobytes < 1024)
            {
                return $"{kilobytes:0.0} KB";
            }

            var megabytes = kilobytes / 1024d;
            if (megabytes < 1024)
            {
                return $"{megabytes:0.0} MB";
            }

            var gigabytes = megabytes / 1024d;
            return $"{gigabytes:0.0} GB";
        }
    }
}
