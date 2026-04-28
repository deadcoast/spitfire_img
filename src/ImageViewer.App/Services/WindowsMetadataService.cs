using System.Runtime.InteropServices.WindowsRuntime;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Windows.Graphics.Imaging;

namespace ImageViewer.App.Services;

public sealed class WindowsMetadataService : IMetadataService
{
    private static readonly string[] PropertyNames =
    [
        "System.Photo.CameraManufacturer",
        "System.Photo.CameraModel",
        "System.ApplicationName",
        "System.Photo.DateTaken",
        "System.Photo.ISOSpeed",
        "System.Photo.FNumber",
        "System.Photo.ExposureTime",
        "System.Photo.FocalLength",
        "System.Photo.Flash",
        "System.GPS.LatitudeDecimal",
        "System.GPS.LongitudeDecimal",
        "System.GPS.Altitude",
        "System.Image.ColorSpace"
    ];

    public async Task<Result<ExifMetadata>> ExtractExifAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return Result<ExifMetadata>.Failure(ViewerError.MetadataExtractionError("ExtractExif", filePath));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var randomAccessStream = source.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

            try
            {
                var properties = await decoder.BitmapProperties.GetPropertiesAsync(PropertyNames);
                return Result<ExifMetadata>.Success(new ExifMetadata(
                    CameraMake: GetString(properties, "System.Photo.CameraManufacturer"),
                    CameraModel: GetString(properties, "System.Photo.CameraModel"),
                    Software: GetString(properties, "System.ApplicationName"),
                    CaptureDate: GetDateTimeOffset(properties, "System.Photo.DateTaken"),
                    Iso: GetInt(properties, "System.Photo.ISOSpeed"),
                    Aperture: GetDouble(properties, "System.Photo.FNumber"),
                    ShutterSpeed: GetDouble(properties, "System.Photo.ExposureTime"),
                    FocalLength: GetDouble(properties, "System.Photo.FocalLength"),
                    FlashFired: GetBool(properties, "System.Photo.Flash"),
                    Latitude: GetDouble(properties, "System.GPS.LatitudeDecimal"),
                    Longitude: GetDouble(properties, "System.GPS.LongitudeDecimal"),
                    Altitude: GetDouble(properties, "System.GPS.Altitude"),
                    ColorSpace: GetString(properties, "System.Image.ColorSpace")));
            }
            catch
            {
                // Missing or malformed EXIF must not block metadata display.
                return Result<ExifMetadata>.Success(ExifMetadata.Empty);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException or ArgumentException)
        {
            return Result<ExifMetadata>.Failure(ViewerError.MetadataExtractionError("ExtractExif", filePath, ex));
        }
    }

    private static string? GetString(
        BitmapPropertySet properties,
        string key)
    {
        return properties.TryGetValue(key, out var property) && property.Value is not null
            ? property.Value.ToString()
            : null;
    }

    private static int? GetInt(
        BitmapPropertySet properties,
        string key)
    {
        var value = GetValue(properties, key);
        if (value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static double? GetDouble(
        BitmapPropertySet properties,
        string key)
    {
        var value = GetValue(properties, key);
        if (value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static bool? GetBool(
        BitmapPropertySet properties,
        string key)
    {
        var value = GetValue(properties, key);
        if (value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static DateTimeOffset? GetDateTimeOffset(
        BitmapPropertySet properties,
        string key)
    {
        var value = GetValue(properties, key);
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime),
            string text when DateTimeOffset.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }

    private static object? GetValue(
        BitmapPropertySet properties,
        string key)
    {
        return properties.TryGetValue(key, out var property) ? property.Value : null;
    }
}
