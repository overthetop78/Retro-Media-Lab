using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Localization.Extensions;
using System.Windows.Media;
using GWGUI.App.Views.Dialogs.Common;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal sealed class MessageView : BusinessDialogView
{
    private const double MinimumWidth = 240;
    private const double MaximumWidth = 560;
    private const double MaximumHeight = 460;
    private const double IconSize = 32;
    private const double IconGap = 16;
    private const double ButtonWidth = 74;
    private const double ButtonHeight = 24;
    private const double ButtonTextInsets = 16;
    private const double ButtonGap = 8;
    private const double FooterGap = 14;
    private const double FrameBorderLayers = 3;
    private const string IconsResource = "/gwgui.app;component/Theme/OS/Editions/Windows95/Styles/Windows95DialogIcons.xaml";
    private readonly MessageDialog source;
    private Button? defaultButton;

    internal MessageView(MessageDialog source) : base(source)
    {
        this.source = source;
        try
        {
            var footer = new UniformGrid { Rows = 1, HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, FooterGap, 0, 0) };
            foreach (var choice in source.Choices)
            {
                var captured = choice;
                var button = Command(Resource(choice), () => source.Choose(captured), choice == source.DefaultResult);
                button.MinWidth = ButtonWidth;
                button.Height = ButtonHeight;
                button.Margin = new Thickness(ButtonGap / 2, 0, ButtonGap / 2, 0);
                footer.Children.Add(button);
                if (button.IsDefault) defaultButton = button;
            }
            SetDock(footer, Dock.Bottom);
            Children.Add(footer);
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            body.ColumnDefinitions.Add(new ColumnDefinition());
            if (source.MessageIcon != MessageBoxImage.None)
            {
                Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(IconsResource, UriKind.Relative) });
                var icon = new Image { Source = (ImageSource)Resources[source.MessageIcon], Width = IconSize, Height = IconSize,
                    Stretch = Stretch.None, Margin = new Thickness(0, 0, IconGap, 0), VerticalAlignment = VerticalAlignment.Center };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.NearestNeighbor);
                body.Children.Add(icon);
            }
            var text = new TextBlock { Text = source.Message, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(text, 1);
            body.Children.Add(text);
            Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
            Loaded += FocusDefault;
        }
        catch { Dispose(); throw; }
    }

    private static string Resource(MessageBoxResult result) => result switch
    {
        MessageBoxResult.OK => "Common.OK",
        MessageBoxResult.Cancel => "Common.Cancel",
        MessageBoxResult.Yes => "Common.Yes",
        MessageBoxResult.No => "Common.No",
        MessageBoxResult.Abort => "Common.Abort",
        MessageBoxResult.Retry => "Common.Retry",
        MessageBoxResult.Ignore => "Common.Ignore",
        MessageBoxResult.TryAgain => "Common.TryAgain",
        MessageBoxResult.Continue => "Common.Continue",
        _ => throw new ArgumentOutOfRangeException(nameof(result))
    };

    internal static Size PreferredSize(MessageDialog source, FrameworkElement context)
    {
        var dpi = VisualTreeHelper.GetDpi(context).PixelsPerDip;
        var bodyTypeface = new Typeface(new FontFamily(Windows95DesktopMetrics.FontFamily),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var text = new FormattedText(source.Message, CultureInfo.CurrentUICulture, context.FlowDirection,
            bodyTypeface,
            Windows95Metrics.FontSize, Brushes.Black, dpi);
        var caption = new FormattedText(source.Title, CultureInfo.CurrentUICulture, context.FlowDirection,
            new Typeface(new FontFamily(Windows95DesktopMetrics.FontFamily), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            Windows95Metrics.FontSize, Brushes.Black, dpi);
        var chrome = (Windows95Metrics.ResizeBorder.Left + FrameBorderLayers) * 2;
        var margins = ContentMargin.Left + ContentMargin.Right;
        var icon = source.MessageIcon == MessageBoxImage.None ? 0 : IconSize + IconGap;
        var buttonWidth = source.Choices.Select(choice => new FormattedText(LocExtension.Get(Resource(choice)),
            CultureInfo.CurrentUICulture, context.FlowDirection, bodyTypeface,
            Windows95Metrics.FontSize, Brushes.Black, dpi).WidthIncludingTrailingWhitespace + ButtonTextInsets)
            .Append(ButtonWidth).Max();
        var buttons = source.Choices.Count * (buttonWidth + ButtonGap) + margins + chrome;
        var content = text.WidthIncludingTrailingWhitespace + icon + margins + chrome;
        var title = caption.WidthIncludingTrailingWhitespace + Windows95Metrics.CaptionButtonWidth + chrome;
        var width = Math.Clamp(Math.Max(content, title), Math.Max(MinimumWidth, buttons), MaximumWidth);
        text.MaxTextWidth = width - icon - margins - chrome;
        var height = Math.Max(text.Height, source.MessageIcon == MessageBoxImage.None ? 0 : IconSize)
            + margins + FooterGap + ButtonHeight + Windows95Metrics.CaptionHeight + chrome;
        return new Size(Math.Ceiling(width), Math.Min(MaximumHeight, Math.Ceiling(height)));
    }

    private void FocusDefault(object sender, RoutedEventArgs args) => ActivateDefault();
    internal void ActivateDefault() => defaultButton?.Focus();
    protected override void Cancel() => source.Cancel();

    protected override void Detach()
    {
        Loaded -= FocusDefault;
        defaultButton = null;
        Resources.MergedDictionaries.Clear();
    }
}
