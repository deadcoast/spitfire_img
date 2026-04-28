namespace ImageViewer.Core.Models;

using System.Globalization;

public sealed record ExifMetadata(
    string? CameraMake = null,
    string? CameraModel = null,
    string? Software = null,
    DateTimeOffset? CaptureDate = null,
    int? Iso = null,
    double? Aperture = null,
    double? ShutterSpeed = null,
    double? FocalLength = null,
    bool? FlashFired = null,
    double? Latitude = null,
    double? Longitude = null,
    double? Altitude = null,
    string? ColorSpace = null)
{
    public static ExifMetadata Empty { get; } = new();

    public bool HasCameraInfo =>
        !string.IsNullOrWhiteSpace(CameraMake) ||
        !string.IsNullOrWhiteSpace(CameraModel) ||
        !string.IsNullOrWhiteSpace(Software);

    public bool HasCaptureSettings =>
        CaptureDate.HasValue ||
        Iso.HasValue ||
        Aperture.HasValue ||
        ShutterSpeed.HasValue ||
        FocalLength.HasValue ||
        FlashFired.HasValue;

    public bool HasGpsInfo => Latitude.HasValue || Longitude.HasValue || Altitude.HasValue;

    public string? FormattedAperture => Aperture.HasValue ? $"f/{Aperture.Value:0.#}" : null;

    public string? FormattedShutterSpeed
    {
        get
        {
            if (!ShutterSpeed.HasValue)
            {
                return null;
            }

            return ShutterSpeed.Value < 1
                ? $"1/{Math.Round(1 / ShutterSpeed.Value):0} s"
                : $"{ShutterSpeed.Value:0.#} s";
        }
    }

    public string? FormattedFocalLength => FocalLength.HasValue ? $"{FocalLength.Value:0.#} mm" : null;

    public string? FormattedCaptureDate =>
        CaptureDate.HasValue
            ? CaptureDate.Value.DateTime.ToString("MMM d, yyyy, h:mm tt", CultureInfo.CurrentCulture)
            : null;

    public string? FormattedAltitude => Altitude.HasValue ? $"{Altitude.Value:0.#} m" : null;

    public string? FormattedFlashStatus => FlashFired switch
    {
        true => "Flash fired",
        false => "No flash",
        _ => null
    };

    public string? FormattedGpsCoordinates
    {
        get
        {
            if (!Latitude.HasValue || !Longitude.HasValue)
            {
                return null;
            }

            var latitudeSuffix = Latitude.Value >= 0 ? "N" : "S";
            var longitudeSuffix = Longitude.Value >= 0 ? "E" : "W";
            return $"{Math.Abs(Latitude.Value):0.000000} {latitudeSuffix}, {Math.Abs(Longitude.Value):0.000000} {longitudeSuffix}";
        }
    }
}
