using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GWGUI.App.Enums.Dialogs;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Common;

namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal sealed class ErrorView : BusinessDialogView
{
    private const double IconSize = 32;
    private const double IconGap = 16;
    private const double DetailLabelWidth = 140;
    private const string IconsResource = "/gwgui.app;component/Theme/OS/Editions/Windows95/Styles/Windows95DialogIcons.xaml";
    private static readonly Thickness RowMargin = new(0, 4, 0, 0);

    internal ErrorView(CommonErrorDialog source) : base(source)
    {
        try
        {
            Footer(Command("Common.OK", () => ThemeDialogCompletion.Complete(source, true), true));
            var data = source.PresentationContent;
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(IconsResource, UriKind.Relative) });
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            body.ColumnDefinitions.Add(new ColumnDefinition());
            var image = new Image { Width = IconSize, Height = IconSize, Stretch = Stretch.None,
                VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, IconGap, 0),
                Source = (ImageSource)Resources[data.Icon == CommonErrorDialog.InformationIcon ? MessageBoxImage.Information : MessageBoxImage.Error] };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            body.Children.Add(image);
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = data.Message, TextWrapping = TextWrapping.Wrap });
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
        CommonErrorDialogMediaIcon.Cassette => "Common.MediaKind.Tape",
        _ => throw new ArgumentOutOfRangeException(nameof(media))
    };

    protected override void Cancel() => ThemeDialogCompletion.Complete(Source, true);
    protected override void Detach() => Resources.MergedDictionaries.Clear();
}
