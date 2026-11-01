using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Services.Maintenance;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
using GWGUI.App.Views.Controls.Shell;
using GWGUI.App.Views.Controls.Tools;
using GWGUI.Infrastructure.Commands.Building;
using GWGUI.Infrastructure.Settings;
using GWGUI.Tests.Application.TestInfrastructure;

namespace GWGUI.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ToolsViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task IndependentInputsReachTheExistingCommandBuilderAndSelectedAction() => sta.Run(() =>
    {
        var source = new ToolsTabSection();
        var controller = new MaintenanceToolsController(source, () => new AppSettings(), new GwCommandBuilder(),
            () => null, () => null, () => false, _ => { }, (key, _) => key);
        var eraseCalls = 0;
        var cleanCalls = 0;
        source.EraseRequested += (_, _) => eraseCalls++;
        source.CleanRequested += (_, _) => cleanCalls++;
        var originalParent = source.EraseTracksValue.Parent;
        using var view = new ToolsView(source, new ApplicationStatusBar(), () => { });
        Field<CheckBox>(view, "EraseTracksEnabled").IsChecked = true;
        Field<TextBox>(view, "EraseTracks").Text = "c=0-39:h=0";
        Field<CheckBox>(view, "EraseRevolutionsEnabled").IsChecked = true;
        Field<TextBox>(view, "EraseRevolutions").Text = "5";
        Assert.Contains("--tracks c=0-39:h=0", controller.BuildErase().ToDisplayString());
        Assert.Contains("--revs 5", controller.BuildErase().ToDisplayString());
        var command = Field<MenuItem>(view, "ExecuteCommand");
        command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, eraseCalls);
        Field<TabControl>(view, "Modes").SelectedIndex = 1;
        Assert.Equal(1, source.ToolsList.SelectedIndex);
        Field<CheckBox>(view, "CleanPassesEnabled").IsChecked = true;
        Field<TextBox>(view, "CleanPasses").Text = "7";
        Assert.Contains("--passes 7", controller.BuildClean().ToDisplayString());
        command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, cleanCalls);
        Assert.Equal(1, eraseCalls);
        source.CleanExecuteButton.Content = "Stop";
        Assert.Equal("Stop", command.Header);
        Assert.Equal("Stop", Field<Button>(view, "CleanExecute").Content);
        source.CleanExecuteButton.IsEnabled = false;
        Assert.False(command.IsEnabled);
        command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, cleanCalls);
        Assert.Same(originalParent, source.EraseTracksValue.Parent);
    });

    [Fact]
    public Task DisposingDetachesBothDirectionsAndCommandsWithoutDestroyingTheSource() => sta.Run(() =>
    {
        var source = new ToolsTabSection();
        var calls = 0;
        source.EraseRequested += (_, _) => calls++;
        var view = new ToolsView(source, new ApplicationStatusBar(), () => calls++);
        var input = Field<TextBox>(view, "EraseTracks");
        var execute = Field<MenuItem>(view, "ExecuteCommand");
        var close = Field<MenuItem>(view, "CloseCommand");
        try
        {
            source.EraseTracksValue.Text = "before";
            Assert.Equal("before", input.Text);
        }
        finally { view.Dispose(); }
        source.EraseTracksValue.Text = "after";
        Assert.Equal("before", input.Text);
        input.Text = "detached";
        Assert.Equal("after", source.EraseTracksValue.Text);
        execute.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        close.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(0, calls);
        source.EraseExecuteButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(1, calls);
        Assert.Null(view.Content);
    });

    private static T Field<T>(ToolsView view, string name) => Assert.IsType<T>(view.FindName(name));
}
