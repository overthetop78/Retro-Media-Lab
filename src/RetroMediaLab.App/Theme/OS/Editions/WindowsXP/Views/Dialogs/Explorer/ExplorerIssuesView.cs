using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Views.Dialogs.Explorer;

namespace RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class ExplorerIssuesView : BusinessDialogView
{
    internal ExplorerIssuesView(ExplorerIssuesWindow source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
    {
        try
        {
            Footer(Command("Common.Close", source.Close, true));
            var issues = new StackPanel();
            foreach (var issue in source.PresentationIssues)
                issues.Children.Add(new TextBlock { Text = issue, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 4, 4, 8) });
            Children.Add(new ScrollViewer { Content = issues, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        }
        catch { Dispose(); throw; }
    }

    protected override void Cancel() => Source.Close();
}
