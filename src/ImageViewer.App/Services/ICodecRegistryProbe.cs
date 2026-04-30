namespace ImageViewer.App.Services;

public interface ICodecRegistryProbe
{
    bool IsHeicCodecRegistered();

    bool IsAvifCodecRegistered();
}
