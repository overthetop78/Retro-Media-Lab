using System.IO;
using System.Windows;
using GWGUI.App.Contracts.Services.Dialogs;
using GWGUI.App.Enums.Services.Dialogs;
using GWGUI.App.Functions.Localization;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Services.Dialogs;
using GWGUI.App.Theme.Common;
using Microsoft.Win32;

namespace GWGUI.App.Views.Dialogs.Common;

/// <summary>Owns file selection state; a theme supplies the visual tree.</summary>
internal sealed class FileSelectionDialog : Window
{
    private const string AllFilesResource = "FileDialog.AllFiles";
    private const string InvalidNameResource = "FileDialog.InvalidName";
    private const string OverwriteResource = "FileDialog.OverwriteConfirm";
    private readonly FileDialog? fileDialog;
    private readonly OpenFolderDialog? folderDialog;
    private readonly Func<string, bool> confirmOverwrite;
    private CancellationTokenSource? enumeration;
    private Task operation = Task.CompletedTask;
    private Task? closing;
    private bool closeRequested;
    private bool accepting;
    private string? customPattern;

    internal FileSelectionKind Kind { get; }
    internal IReadOnlyList<FileSelectionFilter> Filters { get; }
    internal int FilterIndex { get; private set; }
    internal string CurrentDirectory { get; private set; }
    internal string FileName { get; set; }
    internal string? SelectedPath { get; private set; }
    internal IReadOnlyList<FileInfo> Files { get; private set; } = [];
    internal IReadOnlyList<DirectoryInfo> Directories { get; private set; } = [];
    internal IReadOnlyList<DriveInfo> Drives { get; private set; } = [];
    internal bool ShowReadOnly => fileDialog is OpenFileDialog { ShowReadOnly: true };
    internal bool ReadOnly { get; set; }
    internal bool Busy { get; private set; }
    internal string Error { get; private set; } = string.Empty;
    internal Task PresentationOperation => closing is null ? operation : Task.WhenAll(operation, closing);
    internal event EventHandler? Changed;

    internal FileSelectionDialog(FileDialog dialog, Func<string, bool>? confirmOverwrite = null)
    {
        fileDialog = dialog;
        this.confirmOverwrite = confirmOverwrite ?? ConfirmOverwrite;
        Kind = dialog is SaveFileDialog ? FileSelectionKind.Save : FileSelectionKind.Open;
        Title = string.IsNullOrWhiteSpace(dialog.Title)
            ? LocExtension.Get(Kind == FileSelectionKind.Save ? "FileDialog.SaveAs" : "FileDialog.Open") : dialog.Title;
        Filters = FileSelectionFilter.Parse(dialog.Filter, LocExtension.Get(AllFilesResource));
        FilterIndex = Math.Clamp(dialog.FilterIndex - 1, 0, Filters.Count - 1);
        CurrentDirectory = InitialDirectory(dialog.InitialDirectory);
        FileName = dialog.FileName;
        if (Path.IsPathFullyQualified(FileName) && Path.GetDirectoryName(FileName) is { } directory && Directory.Exists(directory))
        {
            CurrentDirectory = directory;
            FileName = Path.GetFileName(FileName);
        }
        ReadOnly = dialog is OpenFileDialog { ReadOnlyChecked: true };
        ConfigureLifetime();
    }

    internal FileSelectionDialog(OpenFolderDialog dialog)
    {
        folderDialog = dialog;
        confirmOverwrite = ConfirmOverwrite;
        Kind = FileSelectionKind.Folder;
        Title = string.IsNullOrWhiteSpace(dialog.Title) ? LocExtension.Get("FileDialog.SelectFolder") : dialog.Title;
        Filters = [new(LocExtension.Get(AllFilesResource), [FileSelectionFilter.AllPattern])];
        CurrentDirectory = InitialDirectory(dialog.InitialDirectory);
        FileName = dialog.FolderName;
        if (Directory.Exists(FileName)) { CurrentDirectory = Path.GetFullPath(FileName); FileName = string.Empty; }
        ConfigureLifetime();
    }

    private static string InitialDirectory(string path) => Directory.Exists(path) ? Path.GetFullPath(path) : Environment.CurrentDirectory;

    private void ConfigureLifetime()
    {
        ResizeMode = ResizeMode.NoResize;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    internal Task InitializeAsync() => NavigateAsync(CurrentDirectory);

    internal Task NavigateAsync(string path)
    {
        if (closeRequested) return operation;
        string fullPath;
        try { fullPath = Path.GetFullPath(path, CurrentDirectory); }
        catch (Exception error) when (IsPathError(error)) { SetError(error); return Task.CompletedTask; }
        enumeration?.Cancel();
        var previous = operation;
        var cancellation = new CancellationTokenSource();
        enumeration = cancellation;
        operation = EnumerateAsync(previous, fullPath, cancellation);
        return operation;
    }

    internal Task SelectFilterAsync(int index)
    {
        if (index < 0 || index >= Filters.Count) throw new ArgumentOutOfRangeException(nameof(index));
        FilterIndex = index;
        customPattern = null;
        return NavigateAsync(CurrentDirectory);
    }

    private async Task EnumerateAsync(Task previous, string directory, CancellationTokenSource cancellation)
    {
        Busy = true;
        Error = string.Empty;
        Changed?.Invoke(this, EventArgs.Empty);
        var filter = customPattern is null ? Filters[FilterIndex]
            : new FileSelectionFilter(customPattern, customPattern.Split(FileSelectionFilter.PatternSeparator));
        try
        {
            await previous;
            var snapshot = await Task.Run(() =>
            {
                var token = cancellation.Token;
                token.ThrowIfCancellationRequested();
                var root = new DirectoryInfo(directory);
                var directories = new List<DirectoryInfo>();
                var files = new List<FileInfo>();
                foreach (var item in root.EnumerateFileSystemInfos())
                {
                    token.ThrowIfCancellationRequested();
                    if (item is DirectoryInfo folder) directories.Add(folder);
                    else if (Kind != FileSelectionKind.Folder && item is FileInfo file && filter.Matches(file.Name)) files.Add(file);
                }
                token.ThrowIfCancellationRequested();
                return (Directories: directories.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(),
                    Files: files.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(), Drives: DriveInfo.GetDrives());
            }, cancellation.Token);
            if (cancellation.IsCancellationRequested || closeRequested) return;
            CurrentDirectory = directory;
            Directories = snapshot.Directories;
            Files = snapshot.Files;
            Drives = snapshot.Drives;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error) when (IsPathError(error))
        {
            if (!cancellation.IsCancellationRequested && !closeRequested) Error = ExceptionDescriptionFunctions.Describe(error);
        }
        finally
        {
            if (ReferenceEquals(enumeration, cancellation))
            {
                enumeration = null;
                Busy = false;
                if (!closeRequested) Changed?.Invoke(this, EventArgs.Empty);
            }
            cancellation.Dispose();
        }
    }

    internal async Task AcceptAsync()
    {
        if (accepting || closeRequested) return;
        accepting = true;
        try
        {
            await operation;
            if (closeRequested) return;
            if (Kind != FileSelectionKind.Folder && FileName.IndexOfAny(FileSelectionFilter.Wildcards) >= 0)
            {
                var path = Path.GetDirectoryName(FileName);
                customPattern = Path.GetFileName(FileName);
                await NavigateAsync(string.IsNullOrWhiteSpace(path) ? CurrentDirectory : path);
                return;
            }
            if (Kind != FileSelectionKind.Folder && string.IsNullOrWhiteSpace(FileName))
            { SetError(LocExtension.Get(InvalidNameResource)); return; }
            var selected = Kind == FileSelectionKind.Folder && string.IsNullOrWhiteSpace(FileName)
                ? CurrentDirectory : Path.GetFullPath(FileName, CurrentDirectory);
            if (Kind == FileSelectionKind.Folder)
            {
                if (!Directory.Exists(selected)) throw new DirectoryNotFoundException(selected);
            }
            else
            {
                if (Directory.Exists(selected)) { FileName = string.Empty; await NavigateAsync(selected); return; }
                if (fileDialog!.ValidateNames && (Path.GetFileName(selected).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                    || string.IsNullOrWhiteSpace(Path.GetFileName(selected))))
                { SetError(LocExtension.Get(InvalidNameResource)); return; }
                if (fileDialog.ValidateNames && fileDialog.AddExtension && !Path.HasExtension(selected))
                {
                    var extensions = Filters[FilterIndex].Extensions;
                    if (!string.IsNullOrWhiteSpace(fileDialog.DefaultExt)) extensions = extensions.Prepend(fileDialog.DefaultExt);
                    foreach (var extension in extensions)
                    {
                        var candidate = extension.IndexOfAny(FileSelectionFilter.Wildcards) >= 0 ? selected
                            : Path.ChangeExtension(selected, extension);
                        if (fileDialog.CheckFileExists && !File.Exists(candidate)) continue;
                        selected = candidate;
                        break;
                    }
                }
                if (fileDialog.CheckPathExists && !Directory.Exists(Path.GetDirectoryName(selected))) throw new DirectoryNotFoundException(selected);
                if (fileDialog.CheckFileExists && !File.Exists(selected)) throw new FileNotFoundException(null, selected);
                if (fileDialog is SaveFileDialog { OverwritePrompt: true } && File.Exists(selected) && !confirmOverwrite(selected)) return;
            }
            SelectedPath = selected;
            if (fileDialog is not null)
            {
                fileDialog.FileName = selected;
                fileDialog.FilterIndex = FilterIndex + 1;
                if (fileDialog is OpenFileDialog open) open.ReadOnlyChecked = ReadOnly;
            }
            else folderDialog!.FolderName = selected;
            ThemeDialogCompletion.Complete(this, true);
        }
        catch (Exception error) when (IsPathError(error)) { SetError(error); }
        finally { accepting = false; }
    }

    private static bool IsPathError(Exception error) => error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException;
    private bool ConfirmOverwrite(string path) => WpfMessageDialogService.ShowMessage(this, LocExtension.Get(OverwriteResource, path), Title,
        MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
    private void SetError(Exception error) => SetError(ExceptionDescriptionFunctions.Describe(error));
    private void SetError(string message) { Error = message; Changed?.Invoke(this, EventArgs.Empty); }

    internal void RequestPresentationClose()
    {
        if (closeRequested) return;
        closeRequested = true;
        enumeration?.Cancel();
        if (operation.IsCompleted) Close();
        else closing = CloseAfterEnumerationAsync();
    }

    private async Task CloseAfterEnumerationAsync()
    {
        try { await operation; }
        finally { Close(); }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs args)
    {
        if (operation.IsCompleted) return;
        args.Cancel = true;
        RequestPresentationClose();
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        Closing -= OnClosing;
        Closed -= OnClosed;
        Changed = null;
    }
}
