using System.Windows;

namespace GWGUI.App.Views.Dialogs.Explorer;

public partial class ExplorerIssuesWindow : Window
{
    internal IReadOnlyList<string> PresentationIssues { get; }
    public ExplorerIssuesWindow(IReadOnlyList<string> issues)
    {
        InitializeComponent();
        PresentationIssues = issues;
        IssuesList.ItemsSource = issues;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
