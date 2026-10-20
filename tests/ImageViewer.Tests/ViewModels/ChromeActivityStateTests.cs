using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class ChromeActivityStateTests
{
    [Fact]
    public void PointerActivity_WhileVisible_DoesNotRequestAnotherReveal()
    {
        var state = new ChromeActivityState { HasImage = true };
        for (var i = 0; i < 100; i++)
            Assert.False(state.NotifyActivity());
        Assert.True(state.TryHide());
        Assert.True(state.NotifyActivity());
        Assert.False(state.NotifyActivity());
    }

    [Fact]
    public void EmptyViewer_DoesNotHideItsOpenAction()
    {
        Assert.False(new ChromeActivityState().TryHide());
    }

    [Fact]
    public void OpenMenuOrKeyboardFocus_HoldsControlsUntilInteractionEnds()
    {
        var state = new ChromeActivityState { HasImage = true, IsInteractionHeld = true };
        Assert.False(state.TryHide());
        state.IsInteractionHeld = false;
        Assert.True(state.TryHide());
        Assert.False(state.TryHide());
    }
}
