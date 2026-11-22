using Microsoft.Win32;
using RetroMediaLab.App.Contracts.Services.Dialogs;
using RetroMediaLab.App.Enums.Services.Dialogs;
using System.IO;

namespace RetroMediaLab.App.Services.Dialogs;

internal static class FileSelectionFiles
{
    internal enum SelectionAction { Select, Navigate, Filter, InvalidName }

    internal sealed record Selection(SelectionAction Action, string Path, string? Pattern = null, bool ConfirmOverwrite = false);

    internal static string ExistingDirectoryOrEmpty(string path) => Directory.Exists(path) ? path : string.Empty;

    internal static (string? Directory, string FileName) InitialLocation(string path)
    {
        if (File.Exists(path)) return (Path.GetDirectoryName(path), Path.GetFileName(path));
        return (Directory.Exists(path) ? path : null, string.Empty);
    }

    internal static Selection ResolveSelection(string name, string directory, FileSelectionKind kind,
        FileSelectionFilter filter, FileDialog? fileDialog)
    {
        if (kind != FileSelectionKind.Folder && name.IndexOfAny(FileSelectionFilter.Wildcards) >= 0)
        {
            var path = Path.GetDirectoryName(name);
            return new(SelectionAction.Filter, string.IsNullOrWhiteSpace(path) ? directory : path, Path.GetFileName(name));
        }
        if (kind != FileSelectionKind.Folder && string.IsNullOrWhiteSpace(name))
            return new(SelectionAction.InvalidName, string.Empty);

        var selected = kind == FileSelectionKind.Folder && string.IsNullOrWhiteSpace(name)
            ? directory : Path.GetFullPath(name, directory);
        if (kind == FileSelectionKind.Folder)
        {
            if (!Directory.Exists(selected)) throw new DirectoryNotFoundException(selected);
            return new(SelectionAction.Select, selected);
        }

        if (Directory.Exists(selected)) return new(SelectionAction.Navigate, selected);
        var fileName = Path.GetFileName(selected);
        if (fileDialog!.ValidateNames && (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || string.IsNullOrWhiteSpace(fileName)))
            return new(SelectionAction.InvalidName, selected);
        if (fileDialog.ValidateNames && fileDialog.AddExtension && !Path.HasExtension(selected))
            selected = ResolveExtension(selected, filter, fileDialog.DefaultExt, fileDialog.CheckFileExists);
        if (fileDialog.CheckPathExists && !Directory.Exists(Path.GetDirectoryName(selected))) throw new DirectoryNotFoundException(selected);
        if (fileDialog.CheckFileExists && !File.Exists(selected)) throw new FileNotFoundException(null, selected);
        var confirm = fileDialog is SaveFileDialog { OverwritePrompt: true } && File.Exists(selected);
        return new(SelectionAction.Select, selected, ConfirmOverwrite: confirm);
    }

    internal static Task<(DirectoryInfo[] Directories, FileInfo[] Files, DriveInfo[] Drives)> EnumerateAsync(
        string directory, FileSelectionKind kind, FileSelectionFilter filter, CancellationToken token) =>
        Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            var root = new DirectoryInfo(directory);
            var directories = new List<DirectoryInfo>();
            var files = new List<FileInfo>();
            foreach (var item in root.EnumerateFileSystemInfos())
            {
                token.ThrowIfCancellationRequested();
                if (item is DirectoryInfo folder) directories.Add(folder);
                else if (kind != FileSelectionKind.Folder && item is FileInfo file && filter.Matches(file.Name)) files.Add(file);
            }
            token.ThrowIfCancellationRequested();
            return (Directories: directories.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(),
                Files: files.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(), Drives: DriveInfo.GetDrives());
        }, token);

    private static string ResolveExtension(string selected, FileSelectionFilter filter, string defaultExtension, bool requireExisting)
    {
        var extensions = filter.Extensions;
        if (!string.IsNullOrWhiteSpace(defaultExtension)) extensions = extensions.Prepend(defaultExtension);
        foreach (var extension in extensions)
        {
            var candidate = extension.IndexOfAny(FileSelectionFilter.Wildcards) >= 0 ? selected
                : Path.ChangeExtension(selected, extension);
            if (requireExisting && !File.Exists(candidate)) continue;
            return candidate;
        }
        return selected;
    }
}
