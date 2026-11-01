using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Windows.About;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class AboutView : BusinessDialogView
{
    private const double LogoSize = 48;
    private const double TitleSize = 14;
    private const string LogoResource = "pack://application:,,,/retromedialab.app;component/Assets/app-icon.png";
    private static readonly Thickness SectionMargin = new(0, 8, 0, 0);
    private readonly ItemsControl components = new();

    internal AboutView(AboutWindow source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
    {
        try
        {
            Footer(Command("Common.OK", source.Close, true));
            var heading = new DockPanel();
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(LogoResource, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            var logo = new Image { Source = image,
                Width = LogoSize, Height = LogoSize, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 12, 0) };
            RenderOptions.SetBitmapScalingMode(logo, BitmapScalingMode.NearestNeighbor);
            SetDock(logo, Dock.Left);
            heading.Children.Add(logo);
            var identity = new StackPanel();
            var title = Label("App.Title");
            title.FontWeight = FontWeights.Bold;
            title.FontSize = TitleSize;
            identity.Children.Add(title);
            var version = new TextBlock();
            Bindings.OneWay(source.PresentationVersion, TextBlock.TextProperty, version, TextBlock.TextProperty);
            identity.Children.Add(version);
            heading.Children.Add(identity);
            SetDock(heading, Dock.Top);
            Children.Add(heading);
            var description = new StackPanel { Margin = SectionMargin };
            description.Children.Add(Label("About.Description"));
            description.Children.Add(Label("About.License"));
            SetDock(description, Dock.Top);
            Children.Add(description);
            var group = new GroupBox { Margin = SectionMargin };
            group.SetBinding(HeaderedContentControl.HeaderProperty, LocExtension.CreateBinding("About.Components"));
            var row = new FrameworkElementFactory(typeof(DockPanel));
            var number = new FrameworkElementFactory(typeof(TextBlock));
            number.SetValue(DockProperty, Dock.Right);
            number.SetValue(FrameworkElement.MarginProperty, new Thickness(12, 0, 0, 0));
            number.SetBinding(TextBlock.TextProperty, new Binding("Version"));
            row.AppendChild(number);
            var name = new FrameworkElementFactory(typeof(TextBlock));
            name.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            row.AppendChild(name);
            components.ItemTemplate = new DataTemplate { VisualTree = row };
            components.ItemsSource = source.PresentationComponents;
            group.Content = new ScrollViewer { Content = components, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            Children.Add(group);
        }
        catch { Dispose(); throw; }
    }

    protected override void Cancel() => Source.Close();
    protected override void Detach() => components.ItemsSource = null;
}
