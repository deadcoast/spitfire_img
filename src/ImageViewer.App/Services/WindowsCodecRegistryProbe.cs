using Microsoft.Win32;

namespace ImageViewer.App.Services;

public sealed class WindowsCodecRegistryProbe : ICodecRegistryProbe
{
    private static readonly string[] WicDecoderRegistryPaths =
    [
        @"SOFTWARE\Microsoft\Windows Imaging Component\Decoders",
        @"SOFTWARE\Classes\CLSID"
    ];

    public bool IsHeicCodecRegistered()
    {
        return IsCodecRegistered(
            [".heic", ".heif"],
            ["image/heic", "image/heif"],
            ["heic", "heif", "hevc"]);
    }

    public bool IsAvifCodecRegistered()
    {
        return IsCodecRegistered(
            [".avif"],
            ["image/avif"],
            ["avif", "av1"]);
    }

    private static bool IsCodecRegistered(
        IReadOnlyCollection<string> extensions,
        IReadOnlyCollection<string> mimeTypes,
        IReadOnlyCollection<string> nameFragments)
    {
        return IsCodecRegisteredInHive(RegistryHive.LocalMachine, extensions, mimeTypes, nameFragments)
            || IsCodecRegisteredInHive(RegistryHive.CurrentUser, extensions, mimeTypes, nameFragments);
    }

    private static bool IsCodecRegisteredInHive(
        RegistryHive hive,
        IReadOnlyCollection<string> extensions,
        IReadOnlyCollection<string> mimeTypes,
        IReadOnlyCollection<string> nameFragments)
    {
        return IsCodecRegisteredInView(hive, RegistryView.Registry64, extensions, mimeTypes, nameFragments)
            || IsCodecRegisteredInView(hive, RegistryView.Registry32, extensions, mimeTypes, nameFragments);
    }

    private static bool IsCodecRegisteredInView(
        RegistryHive hive,
        RegistryView view,
        IReadOnlyCollection<string> extensions,
        IReadOnlyCollection<string> mimeTypes,
        IReadOnlyCollection<string> nameFragments)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);

        foreach (var path in WicDecoderRegistryPaths)
        {
            using var decoderRoot = baseKey.OpenSubKey(path);
            if (decoderRoot is null)
            {
                continue;
            }

            foreach (var subKeyName in decoderRoot.GetSubKeyNames())
            {
                using var decoderKey = decoderRoot.OpenSubKey(subKeyName);
                if (decoderKey is not null && DecoderMatches(decoderKey, extensions, mimeTypes, nameFragments))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool DecoderMatches(
        RegistryKey decoderKey,
        IReadOnlyCollection<string> extensions,
        IReadOnlyCollection<string> mimeTypes,
        IReadOnlyCollection<string> nameFragments)
    {
        foreach (var valueName in decoderKey.GetValueNames())
        {
            if (ValueMatches(decoderKey.GetValue(valueName), extensions, mimeTypes, nameFragments))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ValueMatches(
        object? registryValue,
        IReadOnlyCollection<string> extensions,
        IReadOnlyCollection<string> mimeTypes,
        IReadOnlyCollection<string> nameFragments)
    {
        return registryValue switch
        {
            string value => TextMatches(value, extensions, mimeTypes, nameFragments),
            string[] values => values.Any(value => TextMatches(value, extensions, mimeTypes, nameFragments)),
            _ => false
        };
    }

    private static bool TextMatches(
        string value,
        IReadOnlyCollection<string> extensions,
        IReadOnlyCollection<string> mimeTypes,
        IReadOnlyCollection<string> nameFragments)
    {
        return extensions.Any(extension => value.Contains(extension, StringComparison.OrdinalIgnoreCase))
            || mimeTypes.Any(mimeType => value.Contains(mimeType, StringComparison.OrdinalIgnoreCase))
            || nameFragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
