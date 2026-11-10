using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Constants.Machine;
using RetroMediaLab.App.Functions.Emulation.Shortcuts;
using RetroMediaLab.Infrastructure.Settings.Emulation;
using RetroMediaLab.App.Contracts.Machine;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Logging;
using RetroMediaLab.App.Services.Terminal;
using RetroMediaLab.App.Views.Controls.Emulation.Machine;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.Infrastructure.Settings;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
namespace RetroMediaLab.Tests.Interface.EmulationViews;
internal static class EmulationInteractionScenarios
{
    public static void ErrorLogAppearsOnceInConsoleAndDetaches()
    {
        var directory = Path.Combine(Path.GetTempPath(), "retromedialab-error-console-" + Guid.NewGuid().ToString("N"));
        var terminal = new TerminalSection();
        var settings = new AppSettings();
        var controller = new TerminalPanelController(terminal, new RowDefinition(), new GridSplitter(), () => settings);
        controller.SetVisibility(false);
        controller.AppendWarning("synthetic warning");
        Assert.False(controller.IsVisible);
        Assert.Equal(TraceEventType.Warning, controller.UnreadSeverity);
        Action<string> append = controller.AppendError;
        ErrorLog.EntryWritten += append;
        try
        {
            var error = new InvalidOperationException("synthetic emulation failure");
            Assert.NotNull(ErrorLog.Write(error, "Atari ST - starting emulation", directory));

            var output = terminal.OutputText;
            Assert.Contains("Atari ST - starting emulation", output, StringComparison.Ordinal);
            Assert.Contains(error.ToString(), output, StringComparison.Ordinal);
            Assert.Equal(1, output.Split("synthetic emulation failure", StringSplitOptions.None).Length - 1);
            Assert.False(controller.IsVisible);
            Assert.Equal(TraceEventType.Error, controller.UnreadSeverity);
            controller.AppendWarning("another warning");
            Assert.Equal(TraceEventType.Error, controller.UnreadSeverity);
            controller.Toggle();
            Assert.True(controller.IsVisible);
            Assert.Null(controller.UnreadSeverity);
            controller.AppendError("already visible");
            Assert.Null(controller.UnreadSeverity);

            ErrorLog.EntryWritten -= append;
            Assert.NotNull(ErrorLog.Write(new InvalidOperationException("after detach"), "detached", directory));
            Assert.DoesNotContain("after detach", terminal.OutputText, StringComparison.Ordinal);
        }
        finally
        {
            ErrorLog.EntryWritten -= append;
            terminal.Content = null;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    public static void ConsoleLayoutIsIndependentForEachMainTab()
    {
        var terminal = new TerminalSection();
        var row = new RowDefinition { Height = new GridLength(230) };
        var settings = new AppSettings();
        var controller = new TerminalPanelController(terminal, row, new GridSplitter(), () => settings);
        try
        {
            controller.SetVisibility(true);
            controller.SelectTab(1);
            Assert.Equal(190, row.Height.Value);
            controller.SetVisibility(false);
            controller.SelectTab(0);
            Assert.True(controller.IsVisible);
            Assert.Equal(230, row.Height.Value);
            controller.SelectTab(1);
            Assert.False(controller.IsVisible);
            controller.SetVisibility(true);
            row.Height = new GridLength(310);
            controller.SelectTab(0);
            controller.SelectTab(1);
            Assert.True(controller.IsVisible);
            Assert.Equal(310, row.Height.Value);
        }
        finally
        {
            terminal.Content = null;
        }
    }

    public static void Commands(bool failure)
    {
        var host = new DockPanel(); var calls = new List<int>(); var errors = new List<Exception>(); var restored = 0;
        Func<Task> Command(int index) => () => { calls.Add(index); return failure ? Task.FromException(new IOException("synthetic command")) : Task.CompletedTask; };
        var shortcuts = EmulationShortcutMap.GlobalShortcuts(new Dictionary<string, string>
        {
            [EmulationShortcutDefaults.QuickSave] = "Ctrl+F6",
            [EmulationShortcutDefaults.OpenSettings] = "F8"
        });
        var bar = new MachineCommandBar(host,new(Command(0),Command(1),Command(2),Command(3),Command(4),Command(5),Command(6),Command(7),Command(8),Command(9),Command(10)),shortcuts,errors.Add,() => restored++);
        var keys = new[] {EmulationResourceKeys.Power,
            EmulationResourceKeys.PauseResume,
            EmulationResourceKeys.SoftReset,
            EmulationResourceKeys.HardReset,
            EmulationResourceKeys.QuickSave,
            EmulationResourceKeys.QuickLoad,
            EmulationResourceKeys.CaptureScreen,
            EmulationResourceKeys.Fullscreen,
            EmulationResourceKeys.Audio,
            EmulationResourceKeys.SwitchControllerPointer,
            EmulationResourceKeys.MachineSettings};
        Button Find(string key) => MachineConfigurationScenarios.Controls<Button>(host).Single(x => x.ToolTip is string tip && tip.Split(Environment.NewLine)[0] == LocExtension.Get(key));
        Assert.EndsWith("Ctrl+F6", (string)Find(EmulationResourceKeys.QuickSave).ToolTip, StringComparison.Ordinal);
        Assert.EndsWith("F8", (string)Find(EmulationResourceKeys.MachineSettings).ToolTip, StringComparison.Ordinal);
        Assert.True(ToolTipService.GetShowOnDisabled(Find(EmulationResourceKeys.QuickLoad)));
        Assert.DoesNotContain(MachineConfigurationScenarios.Controls<TextBlock>(host), block => block.Text == LocExtension.Get(EmulationResourceKeys.QuickSave));
        bar.SetPowered(false);
        foreach(var key in keys) Assert.Equal(key == EmulationResourceKeys.Power || key == EmulationResourceKeys.Fullscreen || key == EmulationResourceKeys.MachineSettings,Find(key).IsEnabled);
        bar.SetPowered(true); bar.SetSavedStateAvailability(true,false);
        Assert.True(Find(EmulationResourceKeys.QuickSave).IsEnabled); Assert.False(Find(EmulationResourceKeys.QuickLoad).IsEnabled);
        bar.SetSavedStateAvailability(true,true); Assert.True(Find(EmulationResourceKeys.QuickLoad).IsEnabled);
        foreach(var key in keys) Find(key).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(Enumerable.Range(0,11),calls); Assert.Equal(11,restored); Assert.Equal(failure?11:0,errors.Count);
        var pause = Assert.IsType<TextBlock>(Find(EmulationResourceKeys.PauseResume).Content); var previous = pause.Text;
        bar.SetPaused(true); Assert.NotEqual(previous,pause.Text); bar.SetPaused(false); Assert.Equal(previous,pause.Text);
        bar.SetInputStatus(true,false);
        Assert.Equal(LocExtension.Get("Emulation.Value.Enabled"),AutomationProperties.GetItemStatus(bar.PointerStatus));
        Assert.Equal(LocExtension.Get("Emulation.Value.Disabled"),AutomationProperties.GetItemStatus(bar.ControllerStatus));
        bar.SetInputStatus(false,true);
        Assert.Equal(LocExtension.Get("Emulation.Value.Disabled"),AutomationProperties.GetItemStatus(bar.PointerStatus));
        Assert.Equal(LocExtension.Get("Emulation.Value.Enabled"),AutomationProperties.GetItemStatus(bar.ControllerStatus));
        bar.SetPowered(false); Assert.False(Find(EmulationResourceKeys.QuickSave).IsEnabled); Assert.False(Find(EmulationResourceKeys.QuickLoad).IsEnabled);
        foreach (var aboveDisplay in new[] { false, true })
        {
            bar.Presentation.StatusAboveDisplay = aboveDisplay;
            using var view = new MachineView();
            view.AttachCommands(bar.Presentation);
            var overlay = aboveDisplay
                ? view.Root.Children.OfType<Border>().Single(border => Grid.GetRow(border) == 1)
                : view.DisplayHost.Children.OfType<Border>().Single(border => Panel.GetZIndex(border) == 1);
            Assert.Equal(aboveDisplay ? 2 : 1, Grid.GetRow(view.DisplayHost));
            Assert.Equal(aboveDisplay ? 3 : 2, Grid.GetRow(view.BottomBar));
            Assert.Equal(aboveDisplay ? 1 : 2, view.DisplayHost.Children.Count);
            if (aboveDisplay) Assert.Equal(GridLength.Auto, view.Root.RowDefinitions[1].Height);
            else
            {
                Assert.Equal(MachinePresentationConstants.StatusMaximumWidth, overlay.Width);
                Assert.Equal(MachinePresentationConstants.StatusOverlayHeight, overlay.Height);
                Assert.Equal(new Thickness(MachinePresentationConstants.StatusOverlayMargin), overlay.Margin);
            }
            Assert.False(overlay.IsHitTestVisible);
            Assert.Equal(aboveDisplay ? HorizontalAlignment.Center : HorizontalAlignment.Right, overlay.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Top, overlay.VerticalAlignment);
            var rows = Assert.IsType<StackPanel>(overlay.Child);
            Assert.Equal(aboveDisplay ? Orientation.Horizontal : Orientation.Vertical, rows.Orientation);
            Assert.Equal(5, rows.Children.Count);
            Assert.DoesNotContain(MachineConfigurationScenarios.Controls<TextBlock>(host), block => ReferenceEquals(block, bar.RendererStatus));
            var sources = new[] { bar.Presentation.RendererStatus, bar.Presentation.SizeStatus, bar.Presentation.FpsStatus, bar.Presentation.FrequencyStatus, bar.Presentation.ExecutionStatus };
            for (var index = 0; index < sources.Length; index++)
            {
                var row = Assert.IsType<DockPanel>(rows.Children[index]);
                if (aboveDisplay) Assert.Equal(MachinePresentationConstants.StatusFieldWidth, row.Width);
                else Assert.True(double.IsNaN(row.Width));
                Assert.Equal(aboveDisplay ? MachinePresentationConstants.StatusLineRowHeight : MachinePresentationConstants.StatusRowHeight, row.Height);
                var icon = Assert.IsType<TextBlock>(row.Children[0]);
                var value = Assert.IsType<TextBlock>(row.Children[1]);
                sources[index].Text = index.ToString();
                Assert.Equal(sources[index].Text, value.Text);
                Assert.Same(icon.Foreground, value.Foreground);
                Assert.Equal(TextWrapping.NoWrap, value.TextWrapping);
            }
            bar.RendererStatus.Text = "OpenGL";
            Assert.Equal("OpenGL", Assert.IsType<TextBlock>(Assert.IsType<DockPanel>(rows.Children[0]).Children[1]).Text);
        }
        Assert.True(RetroMediaLab.App.Functions.Machine.MachinePresentationFunctions.StatusAboveDisplay("acorn", "AcornSystem1"));
        Assert.False(RetroMediaLab.App.Functions.Machine.MachinePresentationFunctions.StatusAboveDisplay("acorn", "AcornSystem5"));
        Assert.False(RetroMediaLab.App.Functions.Machine.MachinePresentationFunctions.StatusAboveDisplay("acorn", "AcornSystem2"));
        Assert.False(RetroMediaLab.App.Functions.Machine.MachinePresentationFunctions.StatusAboveDisplay("other", "AcornSystem1"));
    }
}
