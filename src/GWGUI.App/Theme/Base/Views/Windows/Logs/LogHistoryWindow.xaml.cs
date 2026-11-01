using GWGUI.App.Services.Dialogs;
using GWGUI.App.Localization.Extensions;
using GWGUI.App.Functions.Localization;
using GWGUI.App.Services.Logging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Interfaces.Services.Dialogs;
using GWGUI.App.Contracts.Services.Dialogs;

namespace GWGUI.App.Views.Windows.Logs;

public partial class LogHistoryWindow : Window
{
    private readonly HashSet<Task> _pending = [];
    private readonly IFileDialogService _fileDialogs;
    private CancellationTokenSource? _reading;
    private Task _closing = Task.CompletedTask;
    private bool _closeRequested;
    private const string ClosingContext = "Closing log history";
    internal ListBox PresentationFiles => FilesList;
    internal TextBox PresentationContent => ContentText;
    internal Button PresentationExport => ExportButton;
    internal Task PresentationOperation => Task.WhenAll(_pending.Append(_closing));

    public LogHistoryWindow(string directory, IFileDialogService? fileDialogs = null)
    {
        InitializeComponent();
        _fileDialogs = fileDialogs ?? new WpfFileDialogService(this);
        Closing += WindowClosing;
        FilesList.ItemsSource = Directory.Exists(directory) ? new DirectoryInfo(directory).GetFiles("*.log").Where(file => !file.Name.StartsWith("errors-", StringComparison.OrdinalIgnoreCase)).OrderByDescending(file => file.LastWriteTimeUtc).ToArray() : Array.Empty<FileInfo>();
        FilesList.SelectedIndex = FilesList.Items.Count > 0 ? 0 : -1;
    }

    private async void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_closeRequested) return;
        _reading?.Cancel();
        if (FilesList.SelectedItem is not FileInfo file) { ContentText.Clear(); ExportButton.IsEnabled = false; return; }
        var cancellation = new CancellationTokenSource();
        _reading = cancellation;
        await TrackAsync(() => ReadAsync(file, cancellation));
    }

    private async Task TrackAsync(Func<Task> operation)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending.Add(completion.Task);
        try { await operation(); }
        finally
        {
            _pending.Remove(completion.Task);
            completion.SetResult();
        }
    }

    private async Task ReadAsync(FileInfo file, CancellationTokenSource cancellation)
    {
        try
        {
            string content;
            var canExport = false;
            try { content = await File.ReadAllTextAsync(file.FullName, cancellation.Token); canExport = true; }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return; }
            catch (IOException exception)
            {
                ErrorLog.Write(exception, "Reading log history");
                var detail = ExceptionDescriptionFunctions.Describe(exception);
                content = LocExtension.Get("Error.Unexpected", detail);
            }
            if (_closeRequested || cancellation.IsCancellationRequested) return;
            if (Equals(FilesList.SelectedItem, file)) { ContentText.Text = content; ExportButton.IsEnabled = canExport; }
        }
        finally
        {
            if (ReferenceEquals(_reading, cancellation)) _reading = null;
            cancellation.Dispose();
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
        => await TrackAsync(ExportAsync);

    private async Task ExportAsync()
    {
        if (_closeRequested) return;
        if (FilesList.SelectedItem is not FileInfo file) return;
        var path = _fileDialogs.SaveFile(new SaveFileRequest(LocExtension.Get("Logs.ExportFilter"), Path.ChangeExtension(file.Name, ".txt"), ".txt"));
        if (path is not null) await File.WriteAllTextAsync(path, ContentText.Text);
    }

    internal void RequestPresentationClose()
    {
        if (_closeRequested) return;
        _closeRequested = true;
        _reading?.Cancel();
        _closing = CloseAfterOperationsAsync();
    }

    private async Task CloseAfterOperationsAsync()
    {
        try { await Task.WhenAll(_pending); }
        catch (Exception error) { ErrorLog.Write(error, ClosingContext); }
        finally
        {
            FilesList.SelectionChanged -= FilesList_SelectionChanged;
            ExportButton.Click -= Export_Click;
            Closing -= WindowClosing;
            await Dispatcher.InvokeAsync(Close);
        }
    }

    private void WindowClosing(object? sender, System.ComponentModel.CancelEventArgs args)
    {
        args.Cancel = true;
        RequestPresentationClose();
    }
}
