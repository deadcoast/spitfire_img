using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed record SlideshowAdvanceRequestedEventArgs(int NextIndex, ImageItem NextItem);

public interface ISlideshowService
{
    event EventHandler<SlideshowAdvanceRequestedEventArgs>? AdvanceRequested;

    bool IsActive { get; }

    bool IsPaused { get; }

    Result StartSlideshow(Playlist playlist, int currentIndex, SlideshowConfig config);

    Result StopSlideshow();

    Result PauseSlideshow();

    Result ResumeSlideshow();

    Result UpdateConfig(SlideshowConfig config);

    Result ResetTimer();
}
