using System.Collections.Specialized;
using System.ComponentModel;
using ImageViewer.App.Services.Motion;
using ImageViewer.Core.Models;
using ImageViewer.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ImageViewer.App.Views;

public sealed partial class FadingChrome : UserControl
{
    private readonly DispatcherTimer _fadeTimer = new();
    private readonly ChromeActivityState _activity = new();
    private readonly Dictionary<string, MenuFlyoutItem> _commandItems = [];
    private readonly HashSet<UIElement> _hoveredSurfaces = [];
    private readonly MenuFlyoutSubItem _recentMenu = new() { Text = "Recent images" };
    private MainViewModel? _viewModel;
    private IViewerMotionService? _motionService;
    private int _openMenus;
    private bool _externallyHeld;
    private bool _keyboardFocus;

    public FadingChrome()
    {
        InitializeComponent();
        _fadeTimer.Interval = TimeSpan.FromSeconds(2);
        _fadeTimer.Tick += (_, _) => FadeOutChrome();
        foreach (var surface in new UIElement[] { TopChromeSurface, InfoBar, FrameControl })
        {
            surface.PointerEntered += (_, _) => { _hoveredSurfaces.Add(surface); RefreshHold(); };
            surface.PointerExited += (_, _) => { _hoveredSurfaces.Remove(surface); RefreshHold(); };
        }
        GotFocus += OnFocusChanged;
        LostFocus += OnFocusChanged;
        Unloaded += (_, _) => _fadeTimer.Stop();
    }

    public event EventHandler? OpenFileRequested;
    public event EventHandler<string>? RecentFileRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? AboutRequested;

    public void Attach(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InfoBar.Attach(viewModel);
        FrameControl.Attach(viewModel);
        viewModel.RecentFiles.CollectionChanged += OnRecentFilesChanged;
        viewModel.TransformState.PropertyChanged += OnStateChanged;
        viewModel.PlaylistState.PropertyChanged += OnStateChanged;
        viewModel.ViewerState.PropertyChanged += OnViewerChanged;
        viewModel.ComparisonState.PropertyChanged += OnStateChanged;
        viewModel.AnimationState.PropertyChanged += OnStateChanged;
        viewModel.SlideshowState.PropertyChanged += OnStateChanged;
        viewModel.LayoutState.PropertyChanged += OnStateChanged;
        viewModel.PropertyChanged += OnStateChanged;
        BuildMenus();
        UpdateChromeState();
        ShowChrome();
    }

    public void AttachMotionService(IViewerMotionService motionService)
    {
        _motionService = motionService;
        FrameControl.AttachMotionService(motionService);
    }

    private void BuildMenus()
    {
        if (_viewModel is null) return;
        var file = CreateMenu(FileMenuButton, ["File"]);
        file.Items.Insert(1, _recentMenu);
        file.Items.Add(new MenuFlyoutSeparator());
        var exit = new MenuFlyoutItem { Text = "Exit" };
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        file.Items.Add(exit);
        CreateMenu(ViewMenuButton, ["View", "Layout", "Comparison"]);
        CreateMenu(BrowseMenuButton, ["Navigation"]);
        CreateMenu(ImageMenuButton, ["Image"]);
        var play = CreateMenu(PlayMenuButton, ["Slideshow", "Animation"]);
        var transition = new MenuFlyoutSubItem { Text = "Slideshow transition" };
        foreach (var effect in Enum.GetValues<TransitionEffect>())
        {
            var item = new MenuFlyoutItem { Text = effect.ToString() };
            item.Click += async (_, _) => await _viewModel.SetSlideshowTransitionAsync(effect);
            transition.Items.Add(item);
        }
        play.Items.Add(transition);
        var about = new MenuFlyoutItem { Text = "About spitfire.img" };
        about.Click += (_, _) => AboutRequested?.Invoke(this, EventArgs.Empty);
        ((MenuFlyout)ViewMenuButton.Flyout).Items.Add(about);
        RebuildRecentFilesMenu();
    }

    private MenuFlyout CreateMenu(DropDownButton button, string[] categories)
    {
        var flyout = new MenuFlyout();
        foreach (var category in categories)
        {
            var submenu = category is "Layout" or "Comparison" ? new MenuFlyoutSubItem { Text = category } : null;
            if (submenu is not null) flyout.Items.Add(submenu);
            else if (flyout.Items.Count > 0) flyout.Items.Add(new MenuFlyoutSeparator());
            foreach (var command in _viewModel!.Commands.Where(command => command.Category == category))
            {
                var item = new MenuFlyoutItem { Text = command.Title, KeyboardAcceleratorTextOverride = command.KeyboardShortcut == "Theme" ? "" : command.KeyboardShortcut, Tag = command.Id };
                item.Click += OnCommandClicked;
                if (submenu is null) flyout.Items.Add(item);
                else submenu.Items.Add(item);
                _commandItems[command.Id] = item;
            }
        }
        flyout.Opened += (_, _) => { _openMenus++; RefreshHold(); UpdateChromeState(); };
        flyout.Closed += (_, _) => { _openMenus = Math.Max(0, _openMenus - 1); RefreshHold(); };
        button.Flyout = flyout;
        return flyout;
    }

    private async void OnCommandClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not FrameworkElement { Tag: string id }) return;
        if (id == "file.open") OpenFileRequested?.Invoke(this, EventArgs.Empty);
        else await _viewModel.ExecuteCommandAsync(id);
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e) => DispatcherQueue.TryEnqueue(UpdateChromeState);

    private void OnViewerChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateChromeState();
            if (e.PropertyName == nameof(ViewerState.CurrentImage)) ShowChrome();
        });
    }

    private void UpdateChromeState()
    {
        if (_viewModel is null) return;
        _activity.HasImage = _viewModel.ViewerState.CurrentImage is not null;
        _fadeTimer.Interval = TimeSpan.FromMilliseconds(_viewModel.Settings.FadeDelayMs);
        InfoBar.Visibility = _activity.HasImage ? Visibility.Visible : Visibility.Collapsed;
        PreviousButton.IsEnabled = _viewModel.PlaylistState.CurrentIndex > 0;
        NextButton.IsEnabled = _viewModel.PlaylistState.CurrentIndex < _viewModel.PlaylistState.TotalCount - 1;
        FitButton.IsEnabled = ActualSizeButton.IsEnabled = SearchButton.IsEnabled = _activity.HasImage;
        ChromeStatusText.Text = _viewModel.TransformState.HasUnsavedChanges ? "Unsaved changes" : string.Empty;
        foreach (var (id, item) in _commandItems)
        {
            item.IsEnabled = id switch
            {
                "file.open" or "view.theme" or "view.fullscreen" or "view.command-palette" => true,
                _ when _viewModel.IsSavingTransforms && (id == "file.save" || id.StartsWith("image.", StringComparison.Ordinal) && id != "image.metadata") => false,
                "file.save" => _viewModel.TransformState.HasUnsavedChanges,
                "image.undo" => _viewModel.TransformState.CanUndo,
                "image.redo" => _viewModel.TransformState.CanRedo,
                "layout.grid-density-up" or "layout.grid-density-down" => _viewModel.LayoutState.IsGridActive,
                "navigation.previous" or "navigation.first" => PreviousButton.IsEnabled,
                "navigation.next" or "navigation.last" => NextButton.IsEnabled,
                "comparison.side-by-side" or "comparison.overlay" => _viewModel.PlaylistState.TotalCount >= 2,
                "comparison.exit" or "comparison.sync-zoom" or "comparison.count-two" or "comparison.count-three" or "comparison.count-four" => _viewModel.ComparisonState.IsActive,
                "comparison.swap-overlay" => _viewModel.ComparisonState.IsActive && _viewModel.ComparisonState.Layout == ComparisonLayout.Overlay,
                "slideshow.pause" => _viewModel.SlideshowState.IsActive,
                _ when id.StartsWith("animation.", StringComparison.Ordinal) => _viewModel.AnimationState.IsAnimated,
                _ => _activity.HasImage
            };
        }
    }

    public void ShowChrome()
    {
        _fadeTimer.Stop();
        if (_activity.NotifyActivity())
        {
            ChromeRoot.IsHitTestVisible = true;
            ViewerMotionStoryboard.AnimateOpacity(ChromeRoot, GetMotionPlan(ViewerMotionCategory.ChromeReveal), 1);
        }
        if (_activity.HasImage && !_activity.IsInteractionHeld) _fadeTimer.Start();
    }

    public void PauseFade() { _externallyHeld = true; RefreshHold(); }
    public void ResumeFade() { _externallyHeld = false; RefreshHold(); }

    private void RefreshHold()
    {
        // PointerExited can be caused by hiding the surface itself. It must not
        // manufacture fresh user activity and start a hide/reveal feedback loop.
        var wasHeld = _activity.IsInteractionHeld;
        _activity.IsInteractionHeld = _externallyHeld || _openMenus > 0 || _keyboardFocus || _hoveredSurfaces.Count > 0;
        if (_activity.IsInteractionHeld || wasHeld) ShowChrome();
    }

    private void OnFocusChanged(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var focused = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            _keyboardFocus = focused is Control { FocusState: FocusState.Keyboard } && IsDescendant(focused);
            RefreshHold();
        });
    }

    private bool IsDescendant(DependencyObject? element)
    {
        while (element is not null)
        {
            if (ReferenceEquals(element, this)) return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void FadeOutChrome()
    {
        _fadeTimer.Stop();
        if (!_activity.TryHide()) return;
        ViewerMotionStoryboard.AnimateOpacity(ChromeRoot, GetMotionPlan(ViewerMotionCategory.ChromeIdle), 0,
            completed: () => { if (!_activity.IsVisible) ChromeRoot.IsHitTestVisible = false; });
    }

    private ViewerMotionPlan GetMotionPlan(ViewerMotionCategory category)
    {
        var plan = _motionService?.CreatePlan(category) ?? ViewerMotionService.CreateDefaultPlan(category, false);
        return category == ViewerMotionCategory.ChromeIdle && !plan.IsReducedMotion
            ? plan with { Duration = TimeSpan.FromMilliseconds(_viewModel?.Settings.FadeDurationMs ?? 300) }
            : plan;
    }

    private void OnRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e) => DispatcherQueue.TryEnqueue(RebuildRecentFilesMenu);

    private void RebuildRecentFilesMenu()
    {
        _recentMenu.Items.Clear();
        foreach (var file in _viewModel?.RecentFiles ?? [])
        {
            var item = new MenuFlyoutItem { Text = file.Filename };
            ToolTipService.SetToolTip(item, file.FilePath);
            item.Click += (_, _) => RecentFileRequested?.Invoke(this, file.FilePath);
            _recentMenu.Items.Add(item);
        }
        _recentMenu.IsEnabled = _recentMenu.Items.Count > 0;
    }
}
