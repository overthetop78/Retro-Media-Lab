using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.ViewModels.Conversion;
using RetroMediaLab.App.Views.Windows.Conversion;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class FileMigrationView : BusinessDialogView
{
    private readonly FileMigrationWindow source;
    private readonly ListView losses = new();
    private static readonly Thickness RowMargin = new(0, 0, 0, 8);
    private static readonly Thickness ButtonMargin = new(8, 0, 0, 0);

    internal FileMigrationView(FileMigrationWindow source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
    {
        this.source = source;
        try
        {
            Footer(Command("Common.Close", source.RequestPresentationClose), Forward(source.PresentationExecute));
            var body = new Grid();
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            body.RowDefinitions.Add(new RowDefinition());
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var sourceRow = PathRow("Migration.Source", source.PresentationSource, source.PresentationBrowseSource);
            body.Children.Add(sourceRow);
            var summary = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = RowMargin };
            Bindings.OneWay(source.PresentationSourceSummary, TextBlock.TextProperty, summary, TextBlock.TextProperty);
            Grid.SetRow(summary, 1);
            body.Children.Add(summary);

            var destination = new StackPanel();
            destination.Children.Add(Label("Migration.Destination"));
            var formats = new ComboBox { DisplayMemberPath = source.PresentationTarget.DisplayMemberPath, Margin = RowMargin };
            Bindings.OneWay(source.PresentationTarget, ItemsControl.ItemsSourceProperty, formats, ItemsControl.ItemsSourceProperty);
            Bindings.TwoWay(source.PresentationTarget, Selector.SelectedItemProperty, formats, Selector.SelectedItemProperty);
            Bindings.OneWay(source.PresentationTarget, IsEnabledProperty, formats, IsEnabledProperty);
            destination.Children.Add(formats);
            destination.Children.Add(PathRow("Migration.Output", source.PresentationOutput, source.PresentationBrowseOutput));
            Grid.SetRow(destination, 2);
            body.Children.Add(destination);

            var compatibility = new DockPanel();
            var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = RowMargin };
            Bindings.OneWay(source.PresentationCompatibility, TextBlock.TextProperty, status, TextBlock.TextProperty);
            SetDock(status, Dock.Top);
            compatibility.Children.Add(status);
            var acceptance = new CheckBox { Margin = new Thickness(0, 8, 0, 0) };
            Bindings.OneWay(source.PresentationAcceptance, ContentControl.ContentProperty, acceptance, ContentControl.ContentProperty);
            Bindings.OneWay(source.PresentationAcceptance, VisibilityProperty, acceptance, VisibilityProperty);
            Bindings.TwoWay(source.PresentationAcceptance, ToggleButton.IsCheckedProperty, acceptance, ToggleButton.IsCheckedProperty);
            SetDock(acceptance, Dock.Bottom);
            compatibility.Children.Add(acceptance);
            var columns = new GridView();
            Column(columns, "Migration.Severity", nameof(FileMigrationLossRow.Severity), 130);
            Column(columns, "Migration.Path", nameof(FileMigrationLossRow.Path), 210);
            Column(columns, "Migration.Description", nameof(FileMigrationLossRow.Description), 390);
            losses.View = columns;
            losses.ItemsSource = source.Losses;
            compatibility.Children.Add(losses);
            var group = new GroupBox { Header = Label("Migration.Compatibility"), Content = compatibility };
            Grid.SetRow(group, 3);
            body.Children.Add(group);
            var progress = new ProgressBar { Height = 16, Margin = new Thickness(0, 8, 0, 0) };
            Bindings.OneWay(source.PresentationProgress, VisibilityProperty, progress, VisibilityProperty);
            Bindings.OneWay(source.PresentationProgress, ProgressBar.IsIndeterminateProperty, progress, ProgressBar.IsIndeterminateProperty);
            Grid.SetRow(progress, 4);
            body.Children.Add(progress);
            Children.Add(body);
            Loaded += ViewLoaded;
        }
        catch { Dispose(); throw; }
    }

    private async void ViewLoaded(object sender, RoutedEventArgs args) => await source.InitializePresentationAsync();
    protected override void Cancel() => source.RequestPresentationClose();
    protected override void Detach()
    {
        Loaded -= ViewLoaded;
        losses.ItemsSource = null;
    }

    private FrameworkElement PathRow(string resource, TextBox original, Button browse)
    {
        var row = new DockPanel { Margin = RowMargin };
        var label = Label(resource);
        SetDock(label, Dock.Top);
        row.Children.Add(label);
        var button = Forward(browse);
        button.Margin = ButtonMargin;
        SetDock(button, Dock.Right);
        row.Children.Add(button);
        var path = new TextBox { IsReadOnly = true };
        Bindings.OneWay(original, TextBox.TextProperty, path, TextBox.TextProperty);
        row.Children.Add(path);
        return row;
    }

    private static void Column(GridView view, string resource, string property, double width)
    {
        var header = new TextBlock();
        header.SetBinding(TextBlock.TextProperty, LocExtension.CreateBinding(resource));
        view.Columns.Add(new GridViewColumn { Header = header, DisplayMemberBinding = new Binding(property), Width = width });
    }
}
