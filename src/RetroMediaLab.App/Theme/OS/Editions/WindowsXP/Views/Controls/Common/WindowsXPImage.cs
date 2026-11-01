using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class WindowsXPImage : FrameworkElement
{
    public static readonly DependencyProperty PartProperty = DependencyProperty.Register(nameof(Part),
        typeof(WindowsXPImagePart), typeof(WindowsXPImage),
        new FrameworkPropertyMetadata(WindowsXPImagePart.FrameCaption, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
        value => Enum.IsDefined((WindowsXPImagePart)value));
    public static readonly DependencyProperty SchemeProperty = DependencyProperty.Register(nameof(Scheme),
        typeof(WindowsXPColorScheme), typeof(WindowsXPImage),
        new FrameworkPropertyMetadata(WindowsXPColorScheme.Blue, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
        value => Enum.IsDefined((WindowsXPColorScheme)value));
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State),
        typeof(WindowsXPImageState), typeof(WindowsXPImage),
        new FrameworkPropertyMetadata(WindowsXPImageState.Normal, FrameworkPropertyMetadataOptions.AffectsRender),
        value => Enum.IsDefined((WindowsXPImageState)value));
    public static readonly DependencyProperty WindowActiveProperty = DependencyProperty.RegisterAttached("WindowActive",
        typeof(bool), typeof(WindowsXPImage),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
    private const int ZoneCount = 3;
    private const double CenterDivisor = 2;

    public WindowsXPImagePart Part { get => (WindowsXPImagePart)GetValue(PartProperty); set => SetValue(PartProperty, value); }
    public WindowsXPColorScheme Scheme { get => (WindowsXPColorScheme)GetValue(SchemeProperty); set => SetValue(SchemeProperty, value); }
    public WindowsXPImageState State { get => (WindowsXPImageState)GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public static bool GetWindowActive(DependencyObject target) => (bool)target.GetValue(WindowActiveProperty);
    public static void SetWindowActive(DependencyObject target, bool value) => target.SetValue(WindowActiveProperty, value);

    static WindowsXPImage() => IsEnabledProperty.OverrideMetadata(typeof(WindowsXPImage),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public WindowsXPImage()
    {
        ClipToBounds = true;
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
        SetResourceReference(SchemeProperty, WindowsXPResources.SchemeResource);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var definition = WindowsXPImageCatalog.Definition(Scheme, Part);
        return new Size(definition.FrameWidth, definition.FrameHeight);
    }

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (RenderSize.Width <= 0 || RenderSize.Height <= 0) return;
        var definition = WindowsXPImageCatalog.Definition(Scheme, Part);
        var state = IsEnabled ? State : WindowsXPImageState.Disabled;
        var index = WindowsXPImageCatalog.StateIndex(definition, state, GetWindowActive(this));
        var frame = WindowsXPImageCatalog.Frame(Scheme, Part, index);
        switch (definition.SizingType)
        {
            case WindowsXPImageSizing.Tile:
                var tile = new ImageBrush(frame)
                {
                    TileMode = TileMode.Tile,
                    ViewportUnits = BrushMappingMode.Absolute,
                    Viewport = new Rect(0, 0, frame.PixelWidth, frame.PixelHeight),
                    Stretch = Stretch.Fill
                };
                tile.Freeze();
                drawing.DrawRectangle(tile, null, new Rect(RenderSize));
                break;
            case WindowsXPImageSizing.Uniform:
            case WindowsXPImageSizing.TrueSize:
                var ratio = definition.SizingType == WindowsXPImageSizing.Uniform
                    ? Math.Min(1, Math.Min(RenderSize.Width / frame.PixelWidth, RenderSize.Height / frame.PixelHeight)) : 1;
                var width = frame.PixelWidth * ratio;
                var height = frame.PixelHeight * ratio;
                drawing.DrawImage(frame, new Rect((RenderSize.Width - width) / CenterDivisor,
                    (RenderSize.Height - height) / CenterDivisor, width, height));
                break;
            default:
                DrawSlices(drawing, frame, definition.Insets);
                break;
        }
    }

    private void DrawSlices(DrawingContext drawing, BitmapSource frame, Thickness margins)
    {
        var left = Math.Clamp(margins.Left, 0, frame.PixelWidth);
        var right = Math.Clamp(margins.Right, 0, frame.PixelWidth - left);
        var top = Math.Clamp(margins.Top, 0, frame.PixelHeight);
        var bottom = Math.Clamp(margins.Bottom, 0, frame.PixelHeight - top);
        double[] sourceX = [0, left, frame.PixelWidth - right, frame.PixelWidth];
        double[] sourceY = [0, top, frame.PixelHeight - bottom, frame.PixelHeight];
        var horizontalRatio = left + right > RenderSize.Width ? RenderSize.Width / (left + right) : 1;
        var verticalRatio = top + bottom > RenderSize.Height ? RenderSize.Height / (top + bottom) : 1;
        double[] targetX = [0, left * horizontalRatio, RenderSize.Width - right * horizontalRatio, RenderSize.Width];
        double[] targetY = [0, top * verticalRatio, RenderSize.Height - bottom * verticalRatio, RenderSize.Height];
        for (var row = 0; row < ZoneCount; row++)
            for (var column = 0; column < ZoneCount; column++)
            {
                var sourceWidth = (int)(sourceX[column + 1] - sourceX[column]);
                var sourceHeight = (int)(sourceY[row + 1] - sourceY[row]);
                var targetWidth = targetX[column + 1] - targetX[column];
                var targetHeight = targetY[row + 1] - targetY[row];
                if (sourceWidth <= 0 || sourceHeight <= 0 || targetWidth <= 0 || targetHeight <= 0) continue;
                var rectangle = new Int32Rect((int)sourceX[column], (int)sourceY[row], sourceWidth, sourceHeight);
                drawing.DrawImage(WindowsXPImageCatalog.Slice(frame, rectangle),
                    new Rect(targetX[column], targetY[row], targetWidth, targetHeight));
            }
    }
}
