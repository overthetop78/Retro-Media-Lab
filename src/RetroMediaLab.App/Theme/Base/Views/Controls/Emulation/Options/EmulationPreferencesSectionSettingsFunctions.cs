using System.Windows;
using RetroMediaLab.App.Services.Dialogs;
using System.Windows.Controls;
using Microsoft.Win32;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

public sealed partial class EmulationPreferencesSection
{
    private async Task BrowseFolderAsync(TextBox target)
    {
        var dialog = new OpenFolderDialog { InitialDirectory = target.Text };
        if (WpfFileDialogService.ShowFolderDialog(dialog, Window.GetWindow(this)) != true) return;
        target.Text = dialog.FolderName;
        await SaveFoldersAsync();
    }

    private Task SaveFoldersAsync() =>
        _preferencesSettings.SaveFoldersAsync(_storageFolder.Text, _captureFolder.Text, _stateFolder.Text);

    private Task SaveShortcutsAsync() => _preferencesSettings.SaveShortcutsAsync(_shortcuts.Rows, _shortcuts.HasErrors);
}

