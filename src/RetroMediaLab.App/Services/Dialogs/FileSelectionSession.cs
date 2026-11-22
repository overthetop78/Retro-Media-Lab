using System.IO;
using RetroMediaLab.App.Contracts.Services.Dialogs;
using RetroMediaLab.App.Enums.Services.Dialogs;
using RetroMediaLab.App.Functions.Localization;
using RetroMediaLab.App.Localization.Extensions;
using Microsoft.Win32;

namespace RetroMediaLab.App.Services.Dialogs;

internal sealed class FileSelectionSession
{
    private const string AllFilesResource = "FileDialog.AllFiles";
    private const string InvalidNameResource = "FileDialog.InvalidName";
    private readonly FileDialog? fileDialog;
    private readonly OpenFolderDialog? folderDialog;
    private readonly Func<string, bool> confirmOverwrite;
    private CancellationTokenSource? enumeration;
    private Task operation = Task.CompletedTask;
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
    internal Task Operation => operation;
    internal bool CloseRequested => closeRequested;
    internal string Title { get; }
    internal event EventHandler? Changed;

    internal FileSelectionSession(FileDialog dialog, Func<string, bool> confirmOverwrite)
    {
        fileDialog = dialog;
        this.confirmOverwrite = confirmOverwrite;
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
    }

    internal FileSelectionSession(OpenFolderDialog dialog, Func<string, bool> confirmOverwrite)
    {
        folderDialog = dialog;
        this.confirmOverwrite = confirmOverwrite;
        Kind = FileSelectionKind.Folder;
        Title = string.IsNullOrWhiteSpace(dialog.Title) ? LocExtension.Get("FileDialog.SelectFolder") : dialog.Title;
        Filters = [new(LocExtension.Get(AllFilesResource), [FileSelectionFilter.AllPattern])];
        CurrentDirectory = InitialDirectory(dialog.InitialDirectory);
        FileName = dialog.FolderName;
        if (Directory.Exists(FileName)) { CurrentDirectory = Path.GetFullPath(FileName); FileName = string.Empty; }
    }

    private static string InitialDirectory(string path) => Directory.Exists(path) ? Path.GetFullPath(path) : Environment.CurrentDirectory;

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
            var snapshot = await FileSelectionFiles.EnumerateAsync(directory, Kind, filter, cancellation.Token);
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

    internal async Task<bool> AcceptAsync()
    {
        if (accepting || closeRequested) return false;
        accepting = true;
        try
        {
            await operation;
            if (closeRequested) return false;
            var selection = FileSelectionFiles.ResolveSelection(FileName, CurrentDirectory, Kind, Filters[FilterIndex], fileDialog);
            switch (selection.Action)
            {
                case FileSelectionFiles.SelectionAction.Filter:
                    customPattern = selection.Pattern;
                    await NavigateAsync(selection.Path);
                    return false;
                case FileSelectionFiles.SelectionAction.Navigate:
                    FileName = string.Empty;
                    await NavigateAsync(selection.Path);
                    return false;
                case FileSelectionFiles.SelectionAction.InvalidName:
                    SetError(LocExtension.Get(InvalidNameResource));
                    return false;
            }
            var selected = selection.Path;
            if (selection.ConfirmOverwrite && !confirmOverwrite(selected)) return false;
            SelectedPath = selected;
            if (fileDialog is not null)
            {
                fileDialog.FileName = selected;
                fileDialog.FilterIndex = FilterIndex + 1;
                if (fileDialog is OpenFileDialog open) open.ReadOnlyChecked = ReadOnly;
            }
            else folderDialog!.FolderName = selected;
            return true;
        }
        catch (Exception error) when (IsPathError(error)) { SetError(error); return false; }
        finally { accepting = false; }
    }

    private static bool IsPathError(Exception error) => error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException;
    private void SetError(Exception error) => SetError(ExceptionDescriptionFunctions.Describe(error));
    private void SetError(string message) { Error = message; Changed?.Invoke(this, EventArgs.Empty); }

    internal void RequestClose()
    {
        closeRequested = true;
        enumeration?.Cancel();
    }
}
