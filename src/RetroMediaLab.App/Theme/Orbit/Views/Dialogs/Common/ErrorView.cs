using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RetroMediaLab.App.Enums.Dialogs;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Common;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class ErrorView : BusinessDialogView
{
    private const double MessageFontSize = 17;
    private const string ErrorBrush = "Orbit.Error";
    private static readonly Thickness MessageMargin = new(0, 0, 0, 18);
    private const double DetailLabelWidth = 140;
    private static readonly Thickness RowMargin = new(0, 4, 0, 0);

    internal ErrorView(CommonErrorDialog source, bool? dark = null) : base(source, dark)
    {
        try
        {
            Footer(Command("Common.OK", () => ThemeDialogCompletion.Complete(source, true), true));
            var data = source.PresentationContent;
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            body.ColumnDefinitions.Add(new ColumnDefinition());
            var text = new StackPanel();
            var message = new TextBlock { Text = data.Message, TextWrapping = TextWrapping.Wrap, FontSize = MessageFontSize, Margin = MessageMargin };
            message.SetResourceReference(TextBlock.ForegroundProperty, data.Icon == CommonErrorDialog.InformationIcon ? OrbitMetrics.TextBrush : ErrorBrush);
            text.Children.Add(message);
            foreach (var detail in data.Details ?? [])
            {
                var row = new Grid { Margin = RowMargin };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DetailLabelWidth) });
                row.ColumnDefinitions.Add(new ColumnDefinition());
                row.Children.Add(new TextBlock { Text = detail.Label, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });
                var value = new TextBlock { Text = detail.Value, TextWrapping = TextWrapping.Wrap };
                Grid.SetColumn(value, 1);
                row.Children.Add(value);
                text.Children.Add(row);
            }
            if (data.MediaIcons is { Count: > 0 })
            {
                var heading = Label("Emulation.Error.RequiredMedia");
                heading.Margin = RowMargin;
                heading.FontWeight = FontWeights.Bold;
                text.Children.Add(heading);
                foreach (var media in data.MediaIcons) text.Children.Add(Label(MediaResource(media)));
            }
            Grid.SetColumn(text, 1);
            body.Children.Add(text);
            Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        }
        catch { Dispose(); throw; }
    }

    private static string MediaResource(CommonErrorDialogMediaIcon media) => media switch
    {
        CommonErrorDialogMediaIcon.Floppy => "Common.MediaKind.Floppy",
        CommonErrorDialogMediaIcon.HardDisk => "Common.MediaKind.HardDisk",
        CommonErrorDialogMediaIcon.CompactDisc => "Common.MediaKind.Optical",
        CommonErrorDialogMediaIcon.Cartridge => "Emulation.Storage.Cartridges",
        CommonErrorDialogMediaIcon.Tape => "Common.MediaKind.Tape",
        _ => throw new ArgumentOutOfRangeException(nameof(media))
    };

    protected override void Cancel() => ThemeDialogCompletion.Complete(Source, true);
    protected override void Detach() => Resources.MergedDictionaries.Clear();
}
