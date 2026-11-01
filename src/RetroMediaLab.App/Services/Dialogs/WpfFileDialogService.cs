using RetroMediaLab.App.Contracts.Services.Dialogs;
using RetroMediaLab.App.Interfaces.Services.Dialogs;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using RetroMediaLab.App.Services.Windows;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Common;

namespace RetroMediaLab.App.Services.Dialogs;

public sealed class WpfFileDialogService(Window owner) : IFileDialogService
{
    public string? OpenFile(OpenFileRequest request)
    {
        var dialog = new OpenFileDialog { Filter = request.Filter };
        var initialDirectory = GetDialogDirectory(request.InitialDirectory);
        if (initialDirectory is not null) dialog.InitialDirectory = initialDirectory;
        if (!string.IsNullOrWhiteSpace(request.FileName)) dialog.FileName = request.FileName;
        return ShowFileDialog(dialog, owner) == true ? dialog.FileName : null;
    }

    public string? SaveFile(SaveFileRequest request)
    {
        var dialog = new SaveFileDialog { Filter = request.Filter, FileName = request.FileName };
        if (!string.IsNullOrWhiteSpace(request.DefaultExtension)) dialog.DefaultExt = request.DefaultExtension;
        return ShowFileDialog(dialog, owner) == true ? dialog.FileName : null;
    }

    public string? SelectFolder(SelectFolderRequest request)
    {
        var dialog = new OpenFolderDialog { Title = request.Title };
        var initialDirectory = GetDialogDirectory(request.InitialDirectory);
        if (initialDirectory is not null) dialog.InitialDirectory = initialDirectory;
        return ShowFolderDialog(dialog, owner) == true ? dialog.FolderName : null;
    }

    internal static bool? ShowFileDialog(FileDialog dialog, Window? owner = null)
        => ShowFileDialog(dialog, owner, (native, nativeOwner) => native.ShowDialog(nativeOwner));

    internal static bool? ShowFileDialog(FileDialog dialog, Window? owner, Func<FileDialog, Window?, bool?> native)
    {
        owner ??= WpfDialogOwner.Resolve();
        var request = new FileSelectionDialog(dialog);
        if (TryShowRequest(owner, request, out var result)) return result;
        return native(dialog, WpfDialogOwner.Resolve(owner) ?? owner);
    }

    internal static bool? ShowFolderDialog(OpenFolderDialog dialog, Window? owner = null)
        => ShowFolderDialog(dialog, owner, (native, nativeOwner) => native.ShowDialog(nativeOwner));

    internal static bool? ShowFolderDialog(OpenFolderDialog dialog, Window? owner, Func<OpenFolderDialog, Window?, bool?> native)
    {
        owner ??= WpfDialogOwner.Resolve();
        var request = new FileSelectionDialog(dialog);
        if (TryShowRequest(owner, request, out var result)) return result;
        return native(dialog, WpfDialogOwner.Resolve(owner) ?? owner);
    }

    private static bool TryShowRequest(Window? owner, FileSelectionDialog request, out bool? result)
    {
        try
        {
            using var ownership = owner is null ? null : ThemeDialogCompletion.AttachOwner(request, owner);
            if (owner is not null && WpfWindowNavigationService.TryShowThemeWindow(owner, request))
            {
                result = ThemeDialogCompletion.Result(request);
                return true;
            }
            result = null;
            return false;
        }
        finally { WpfWindowNavigationService.CloseOwnedDialog(request); }
    }

    internal static string? GetDialogDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return null;
        return NormalizeDialogPath(path);
    }

    internal static string NormalizeDialogPath(string path)
    {
        if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) return @"\\" + path[8..];
        return path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..] : path;
    }
}
