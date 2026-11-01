using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Functions.Views.Emulation.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace RetroMediaLab.App.Views.Dialogs.Emulation.Storage;

internal sealed class MemoryCardDirectoryConfigurationDialog : Window
{
    private readonly TextBox _path = new();
    internal string? SupportPath { get; private set; }
    internal TextBox PresentationPath => _path;
    internal Button PresentationBrowse { get; }
    internal Button PresentationRemove { get; }

    internal MemoryCardDirectoryConfigurationDialog(string identifier, string? currentPath, string defaultFolder)
    {
        Title = LocExtension.Get(EmulationResourceKeys.StorageDeviceConfigure) + " " + identifier;
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Width = 760;
        MinWidth = 580;
        SizeToContent = SizeToContent.Height;
        MaxHeight = SystemParameters.WorkArea.Height;
        _path.Text = currentPath ?? defaultFolder;
        var field = StorageDialogUi.PathField(LocExtension.Get("Emulation.Storage.File.DestinationFolder"), _path, () =>
        {
            var browse = new OpenFolderDialog { InitialDirectory = _path.Text, Title = Title };
            if (WpfFileDialogService.ShowFolderDialog(browse, this) == true) _path.Text = browse.FolderName;
        });
        PresentationBrowse = field.Children.OfType<Button>().Single();
        var footer = StorageDialogUi.Footer(this, LocExtension.Get("Common.Save"), () =>
        {
            try
            {
                var path = Path.GetFullPath(_path.Text);
                Directory.CreateDirectory(path);
                SupportPath = path;
                ThemeDialogCompletion.Complete(this, true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            { WpfMessageDialogService.ShowMessage(this, error.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning); }
        });
        var remove = new Button { Content = LocExtension.Get("Emulation.Storage.Media.Remove"), Margin = new Thickness(0, 0, 8, 0) };
        remove.Click += (_, _) => { SupportPath = null; ThemeDialogCompletion.Complete(this, true); };
        PresentationRemove = remove;
        footer.Children.Insert(0, remove);
        Content = StorageDialogUi.DialogLayout(StorageDialogUi.DialogHeader(IconGlyphs.Memory, Title, identifier), field, footer);
    }
}
