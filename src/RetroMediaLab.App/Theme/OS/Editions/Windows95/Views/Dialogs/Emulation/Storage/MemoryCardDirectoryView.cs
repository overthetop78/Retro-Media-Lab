using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal sealed class MemoryCardDirectoryView : BusinessDialogView
{
    private SettingsFieldView? path;

    internal MemoryCardDirectoryView(MemoryCardDirectoryConfigurationDialog source) : base(source)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(Forward(source.PresentationRemove), Command("Common.Cancel", Cancel), save);
            var body = new StackPanel();
            body.Children.Add(Label("Emulation.Storage.File.DestinationFolder"));
            var row = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
            var browse = Forward(source.PresentationBrowse);
            SetDock(browse, Dock.Right);
            row.Children.Add(browse);
            path = new SettingsFieldView(new EmulationSettingsField(nameof(source.PresentationPath),
                EmulationMachineTab.Storage, string.Empty, string.Empty, EmulationSettingsEditor.DirectoryPath, null),
                source.PresentationPath, () => Task.CompletedTask);
            row.Children.Add(path);
            body.Children.Add(row);
            Children.Add(body);
        }
        catch { Dispose(); throw; }
    }

    protected override void Detach()
    {
        try { path?.Dispose(); }
        finally { path = null; }
    }
}
