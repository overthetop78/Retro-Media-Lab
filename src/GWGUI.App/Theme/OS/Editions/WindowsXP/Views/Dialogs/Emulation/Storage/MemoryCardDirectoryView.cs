using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class MemoryCardDirectoryView : BusinessDialogView
{
    private SettingsFieldView? path;

    internal MemoryCardDirectoryView(MemoryCardDirectoryConfigurationDialog source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
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
