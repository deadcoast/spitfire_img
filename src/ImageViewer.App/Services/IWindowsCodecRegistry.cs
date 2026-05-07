namespace ImageViewer.App.Services;

public interface IWindowsCodecRegistry
{
    bool IsHeicCodecInstalled();

    bool IsAvifCodecInstalled();
}
