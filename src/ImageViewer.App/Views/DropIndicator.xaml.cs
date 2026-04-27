using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
    }
}
