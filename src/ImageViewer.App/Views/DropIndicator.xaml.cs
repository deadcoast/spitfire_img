using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace ImageViewer.App.Views;

public sealed partial class DropIndicator : UserControl
{
    public DropIndicator()
    {
        InitializeComponent();
    }

    public void Show(bool isValid)
    {
        MessageText.Text = isValid ? "Drop image to open" : "Unsupported file format";
        Visibility = Visibility.Visible;
        AnimateOpacity(1, 100);
    }

    public void Hide()
    {
        AnimateOpacity(0, 80);
        Visibility = Visibility.Collapsed;
    }

    private void AnimateOpacity(double opacity, int durationMs)
    {
        var animation = new DoubleAnimation
        {
            To = opacity,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EnableDependentAnimation = true
        };

        Storyboard.SetTarget(animation, this);
        Storyboard.SetTargetProperty(animation, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
}
