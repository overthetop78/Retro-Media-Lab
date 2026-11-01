using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Views.Dialogs.Common;

namespace GWGUI.App.Theme.Orbit.Views;

internal sealed class MessageView : BusinessDialogView
{
    private readonly MessageDialog source;
    private Button? defaultButton;
    private const double MessageWidth = 480;
    private const double MaximumBodyHeight = 420;
    private const double MinimumBodyHeight = 80;
    private const double ActionHeight = 100;

    internal MessageView(MessageDialog source, bool? dark = null) : base(source, dark)
    {
        this.source = source;
        try
        {
            var buttons = source.Choices.Select(choice =>
            {
                var button = Command(Resource(choice), () => source.Choose(choice), choice == source.DefaultResult);
                if (button.IsDefault) defaultButton = button;
                return button;
            }).ToArray();
            Footer(buttons);
            var message = new TextBlock { Text = source.Message, TextWrapping = TextWrapping.Wrap, MaxWidth = MessageWidth };
            Children.Add(new ScrollViewer { Content = message, MaxHeight = MaximumBodyHeight,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
            Loaded += FocusDefault;
        }
        catch { Dispose(); throw; }
    }

    internal static Size PreferredSize(MessageDialog source, FrameworkElement context, bool? dark = null)
    {
        var text = new TextBlock { Text = source.Message, TextWrapping = TextWrapping.Wrap, FontSize = OrbitMetrics.FontSize };
        text.Measure(new Size(MessageWidth, double.PositiveInfinity));
        return new Size(MessageWidth + ContentMargin.Left + ContentMargin.Right,
            Math.Clamp(text.DesiredSize.Height, MinimumBodyHeight, MaximumBodyHeight) + ActionHeight);
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

    internal void ActivateDefault() => defaultButton?.Focus();
    private void FocusDefault(object sender, RoutedEventArgs args) => ActivateDefault();
    protected override void Cancel() => source.Cancel();
    protected override void Detach() { Loaded -= FocusDefault; defaultButton = null; }
}
