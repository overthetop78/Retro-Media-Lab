using System.IO;
using System.Windows;
using Microsoft.Win32;
using RetroMediaLab.App.Contracts.Services.Dialogs;
using RetroMediaLab.App.Enums.Services.Dialogs;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Views.Dialogs.Common;

internal sealed class FileSelectionDialog : Window
{
    private readonly FileSelectionSession session;
    private Task? closing;

    internal FileSelectionKind Kind => session.Kind;
    internal IReadOnlyList<FileSelectionFilter> Filters => session.Filters;
    internal int FilterIndex => session.FilterIndex;
    internal string CurrentDirectory => session.CurrentDirectory;
    internal string FileName { get => session.FileName; set => session.FileName = value; }
    internal string? SelectedPath => session.SelectedPath;
    internal IReadOnlyList<FileInfo> Files => session.Files;
    internal IReadOnlyList<DirectoryInfo> Directories => session.Directories;
    internal IReadOnlyList<DriveInfo> Drives => session.Drives;
    internal bool ShowReadOnly => session.ShowReadOnly;
    internal bool ReadOnly { get => session.ReadOnly; set => session.ReadOnly = value; }
    internal bool Busy => session.Busy;
    internal string Error => session.Error;
    internal Task PresentationOperation => closing is null ? session.Operation : Task.WhenAll(session.Operation, closing);
    internal event EventHandler? Changed;

    internal FileSelectionDialog(FileDialog dialog, Func<string, bool>? confirmOverwrite = null)
    {
        session = new FileSelectionSession(dialog, confirmOverwrite ?? ConfirmOverwrite);
        ConfigureLifetime();
    }

    internal FileSelectionDialog(OpenFolderDialog dialog)
    {
        session = new FileSelectionSession(dialog, ConfirmOverwrite);
        ConfigureLifetime();
    }

    private void ConfigureLifetime()
    {
        Title = session.Title;
        ResizeMode = ResizeMode.NoResize;
        session.Changed += OnSessionChanged;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    internal Task InitializeAsync() => session.InitializeAsync();
    internal Task NavigateAsync(string path) => session.NavigateAsync(path);
    internal Task SelectFilterAsync(int index) => session.SelectFilterAsync(index);

    internal async Task AcceptAsync()
    {
        if (await session.AcceptAsync()) ThemeDialogCompletion.Complete(this, true);
    }

    private bool ConfirmOverwrite(string path) => WpfMessageDialogService.ShowMessage(this,
        LocExtension.Get("FileDialog.OverwriteConfirm", path), Title, MessageBoxButton.YesNo,
        MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;

    internal void RequestPresentationClose()
    {
        if (session.CloseRequested) return;
        session.RequestClose();
        if (session.Operation.IsCompleted) Close();
        else closing = CloseAfterEnumerationAsync();
    }

    private async Task CloseAfterEnumerationAsync()
    {
        try { await session.Operation; }
        finally { Close(); }
    }

    private void OnSessionChanged(object? sender, EventArgs args) => Changed?.Invoke(this, EventArgs.Empty);

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs args)
    {
        if (session.Operation.IsCompleted) return;
        args.Cancel = true;
        RequestPresentationClose();
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        Closing -= OnClosing;
        Closed -= OnClosed;
        session.Changed -= OnSessionChanged;
        Changed = null;
    }
}
