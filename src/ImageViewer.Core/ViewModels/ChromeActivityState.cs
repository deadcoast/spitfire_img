namespace ImageViewer.Core.ViewModels;

/// <summary>Visibility policy, independent of timers and animation frames.</summary>
public sealed class ChromeActivityState
{
    public bool IsVisible { get; private set; } = true;
    public bool HasImage { get; set; }
    public bool IsInteractionHeld { get; set; }

    public bool NotifyActivity()
    {
        var changed = !IsVisible;
        IsVisible = true;
        return changed;
    }

    public bool TryHide()
    {
        if (!HasImage || IsInteractionHeld || !IsVisible)
            return false;
        IsVisible = false;
        return true;
    }
}
