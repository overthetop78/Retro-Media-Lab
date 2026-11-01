using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal partial class Windows31Frame : UserControl, IDisposable
{
    private readonly Windows31WindowPlacement placement;
    private bool disposed;
    private bool isDialog;
    internal bool AllowClose { get; set; } = true;
    internal bool AllowResize { get; set; } = true;

    internal void ConfigureAsDialog()
    {
        isDialog = true;
        IconifyCommand.Visibility = Visibility.Collapsed;
        MaximizeCommand.Visibility = Visibility.Collapsed;
    }
    private const string MaximizePath = "M 0,4 L 8,4 4,0 Z";
    private const string RestorePath = "M 0,4 L 8,4 4,0 Z M 0,6 L 8,6 4,10 Z";

    internal static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(Windows31Frame), new PropertyMetadata(string.Empty));

    internal Windows31Frame(Windows31WindowPlacement placement, FrameworkElement document)
    {
        this.placement = placement;
        try
        {
            InitializeComponent();
            Windows31PaletteResources.Initialize(this);
            DocumentHost.Content = document;
            placement.PropertyChanged += PlacementChanged;
            ApplyPlacement();
        }
        catch { Dispose(); throw; }
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private void PlacementChanged(object? sender, PropertyChangedEventArgs args) => ApplyPlacement();

    private void ApplyPlacement()
    {
        if (disposed) return;
        var bounds = placement.Bounds;
        Width = bounds.Width;
        Height = bounds.Height;
        Canvas.SetLeft(this, bounds.Left);
        Canvas.SetTop(this, bounds.Top);
        Visibility = placement.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        FrameBorder.SetResourceReference(Border.BackgroundProperty, placement.IsActive ? Windows31PaletteResources.ActiveBorder : Windows31PaletteResources.InactiveBorder);
        Caption.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, placement.IsActive ? Windows31PaletteResources.ActiveCaption : Windows31PaletteResources.InactiveCaption);
        CaptionText.SetResourceReference(TextBlock.ForegroundProperty, placement.IsActive ? Windows31PaletteResources.CaptionText : Windows31PaletteResources.InactiveCaptionText);
        MaximizeGlyph.Data = Geometry.Parse(placement.State == Windows31WindowState.Maximized ? RestorePath : MaximizePath);
        if (!placement.IsActive || placement.State != Windows31WindowState.Normal) EndPlacement(false);
        if (!placement.IsActive || !placement.IsVisible) CloseSystemMenu();
    }

    private void ActivateFrame(object sender, MouseButtonEventArgs args)
    {
        placement.Activate();
        if (args.ChangedButton != MouseButton.Left || placement.State != Windows31WindowState.Normal) return;
        var edges = ResizeEdgesAt(args.GetPosition(this));
        if (edges != Windows31ResizeEdges.None && BeginPointerPlacement(edges, args.GetPosition(Parent as IInputElement))) args.Handled = true;
    }

    private void IconifyClick(object sender, RoutedEventArgs args)
    {
        if (!isDialog) placement.Iconify();
        args.Handled = true;
    }

    private void MaximizeClick(object sender, RoutedEventArgs args)
    {
        ToggleMaximize();
        args.Handled = true;
    }

    private void ToggleMaximize()
    {
        if (isDialog) return;
        if (placement.State == Windows31WindowState.Maximized) placement.Restore();
        else placement.Maximize();
    }

    private void CaptionMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (args.OriginalSource is not Grid) return;
        if (args.ClickCount == 2) { EndPlacement(false); ToggleMaximize(); }
        else if (placement.State == Windows31WindowState.Normal) BeginPointerPlacement(Windows31ResizeEdges.None, args.GetPosition(Parent as IInputElement));
        args.Handled = true;
    }

    internal event CancelEventHandler? Closing;

    private void RequestClose()
    {
        if (!AllowClose) return;
        EndPlacement(false);
        CloseSystemMenu();
        var args = new CancelEventArgs();
        Closing?.Invoke(this, args);
        if (!args.Cancel) placement.Close();
    }

    private void SystemDoubleClick(object sender, MouseButtonEventArgs args)
    {
        RequestClose();
        args.Handled = true;
    }

    private void FrameKeyDown(object sender, KeyEventArgs args)
    {
        if (HandlePlacementKey(args)) return;
        if (Keyboard.Modifiers == ModifierKeys.Alt && args.SystemKey == Key.Space)
        {
            ShowSystemMenu();
            args.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Alt && args.SystemKey == Key.F4)
        {
            RequestClose();
            args.Handled = true;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            try { EndPlacement(false); }
            finally { CloseSystemMenu(); }
        }
        finally
        {
            placement.PropertyChanged -= PlacementChanged;
            PreviewMouseDown -= ActivateFrame;
            PreviewKeyDown -= FrameKeyDown;
            PreviewMouseMove -= FrameMouseMove;
            PreviewMouseLeftButtonUp -= FrameMouseUp;
            LostMouseCapture -= FrameLostCapture;
            MouseLeave -= FrameMouseLeave;
            if (Caption is not null) Caption.MouseLeftButtonDown -= CaptionMouseDown;
            if (SystemBox is not null)
            {
                SystemBox.MouseDoubleClick -= SystemDoubleClick;
                SystemBox.Click -= SystemClick;
            }
            if (IconifyCommand is not null) IconifyCommand.Click -= IconifyClick;
            if (MaximizeCommand is not null) MaximizeCommand.Click -= MaximizeClick;
            if (DocumentHost is not null) DocumentHost.Content = null;
            Closing = null;
            Content = null;
        }
    }
}
