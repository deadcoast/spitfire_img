using Microsoft.Win32;

namespace ImageViewer.App.Services;

public sealed class WindowsCodecRegistry : IWindowsCodecRegistry
{
    private const string WicDecoderInstancePath = @"CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}\Instance";

    public bool IsHeicCodecInstalled()
    {
        return HasWicDecoder(["heic", "heif"]);
    }

    public bool IsAvifCodecInstalled()
    {
        return HasWicDecoder(["avif", "av1"]);
    }

    private static bool HasWicDecoder(IReadOnlyCollection<string> tokens)
    {
        using var instances = Registry.ClassesRoot.OpenSubKey(WicDecoderInstancePath);
        if (instances is null)
        {
            return false;
        }

        foreach (var decoderId in instances.GetSubKeyNames())
        {
            using var instanceKey = instances.OpenSubKey(decoderId);
            using var decoderKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{decoderId}");
            if (ContainsToken(instanceKey, tokens) || ContainsToken(decoderKey, tokens))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsToken(RegistryKey? key, IReadOnlyCollection<string> tokens)
    {
        if (key is null)
        {
            return false;
        }

        foreach (var valueName in key.GetValueNames())
        {
            if (key.GetValue(valueName) is string value && Matches(value, tokens))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(string value, IReadOnlyCollection<string> tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
