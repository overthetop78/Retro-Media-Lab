using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.App.Functions.Localization;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Options.Controllers;
using RetroMediaLab.App.Services.Logging;
using RetroMediaLab.App.Services.Updates;
using RetroMediaLab.App.Views.Controls.Options;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace RetroMediaLab.App.Views.Windows.Updates;

public partial class UpdatesWindow : Window
{
    private readonly UpdateOptionsController _controller;
    private readonly PendingModuleInstallationStore _pendingInstallations;
    private readonly Func<MessageBoxResult> _confirmCloseWithPending;
    private bool _finishInProgress;
    private bool _closeRequested;
    private bool _allowClose;
    private Task _closing = Task.CompletedTask;
    internal Task PresentationOperation => Task.WhenAll(_controller.PendingOperations, _closing);
    internal OptionsUpdatesSection Section => UpdatesSection;
    internal Button FooterButton => FooterAction;
    internal TextBlock FooterHint => FinishInstallationHint;
    internal bool RequiresFinishingBeforeClose => _pendingInstallations.Items.Count > 0;

    public UpdatesWindow() : this(new PendingModuleInstallationStore(), null)
    {
    }

    internal UpdatesWindow(PendingModuleInstallationStore pendingInstallations, Action<Exception>? reportError = null, Func<MessageBoxResult>? confirmCloseWithPending = null)
    {
        InitializeComponent();
        _pendingInstallations = pendingInstallations;
        _confirmCloseWithPending = confirmCloseWithPending ?? (() => WpfMessageDialogService.ShowMessage(this, LocExtension.Get("Updates.CloseWithPendingModulesConfirm"), LocExtension.Get("Updates.Title"), MessageBoxButton.YesNo, MessageBoxImage.Question));
        _controller = new UpdateOptionsController(this, UpdatesSection, new ApplicationUpdateService(), (key, arguments) => LocExtension.Get(key, arguments), reportError ?? ShowLoggedError, pendingInstallations: pendingInstallations);
        _pendingInstallations.Changed += PendingInstallationsChanged;
        Closing += WindowClosing;
        Closed += WindowClosed;
        RefreshFooterAction();
    }

    internal void RefreshLocalizedContent()
    {
        Title = LocExtension.Get("Updates.Title");
        UpdatesSection.RefreshLocalizedContent();
        _controller.RefreshLocalizedContent();
        RefreshFooterAction();
    }

    private void ShowLoggedError(Exception exception)
    {
        ErrorLog.Write(exception, "Managing updates");
        var detail = ExceptionDescriptionFunctions.Describe(exception);
        WpfMessageDialogService.ShowMessage(this, LocExtension.Get("Error.Unexpected", detail), LocExtension.Get("Updates.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void FooterAction_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingInstallations.Items.Count == 0)
        {
            RequestPresentationClose();
            return;
        }

        await FinishInstallationAsync();
    }

    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        RequestPresentationClose();
    }

    internal void RequestPresentationClose()
    {
        if (_closeRequested || _finishInProgress && RequiresFinishingBeforeClose) return;
        _closeRequested = true;
        _closing = CloseAfterOperationsAsync();
    }

    private async Task CloseAfterOperationsAsync()
    {
        if (RequiresFinishingBeforeClose)
        {
            if (_confirmCloseWithPending() == MessageBoxResult.Yes)
            {
                await FinishInstallationAsync(confirm: false);
                if (RequiresFinishingBeforeClose) { _closeRequested = false; return; }
            }
            else _pendingInstallations.Clear();
        }
        try { await _controller.StopAsync(); }
        finally
        {
            _allowClose = true;
            await Dispatcher.InvokeAsync(Close);
        }
    }

    private async Task FinishInstallationAsync(bool confirm = true)
    {
        if (_finishInProgress) return;
        _finishInProgress = true;
        FooterAction.IsEnabled = false;
        try { await _controller.FinishModuleInstallationsAsync(confirm); }
        finally
        {
            _finishInProgress = false;
            if (!_allowClose) FooterAction.IsEnabled = true;
        }
    }

    private void PendingInstallationsChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(RefreshFooterAction);
            return;
        }
        RefreshFooterAction();
    }

    private void RefreshFooterAction()
    {
        var pending = _pendingInstallations.Items.Count > 0;
        FooterAction.Content = LocExtension.Get(pending ? "Updates.FinishModuleInstallations" : "Common.Close");
        FinishInstallationHint.Visibility = pending ? Visibility.Visible : Visibility.Collapsed;
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        _pendingInstallations.Changed -= PendingInstallationsChanged;
        Closing -= WindowClosing;
        Closed -= WindowClosed;
        _controller.Dispose();
    }
}
