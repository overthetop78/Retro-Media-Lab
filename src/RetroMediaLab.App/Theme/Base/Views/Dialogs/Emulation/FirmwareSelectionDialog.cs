using System.Globalization;
using RetroMediaLab.App.Theme.Common;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Contracts.Emulation.Firmware;
using RetroMediaLab.App.Factories.Views.Common;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Views.Dialogs.Emulation;

/// <summary>Sélection commune d'un firmware, sans dialogue de fichiers ni règle propre à un module.</summary>
internal sealed class FirmwareSelectionDialog : Window, IAsyncDisposable
{
    private readonly EmulationFirmwareSettingsContent _settings;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly ListBox _firmwares = new();
    private readonly Button _save = new() { IsDefault = true, MinWidth = 90 };
    private readonly Button _refresh;
    private readonly Button _open;
    private bool _closing;
    private readonly TextBlock _empty = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _path = new() { TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 0, 8), Opacity = 0.7, TextWrapping = TextWrapping.Wrap };
    private Task _refreshTask = Task.CompletedTask;
    private bool _busy;
    private bool _disposed;

    internal EmulationFirmwareCandidate? SelectedFirmware { get; private set; }

    internal ListBox PresentationList => _firmwares;
    internal Button PresentationSave => _save;
    internal Button PresentationRefresh => _refresh;
    internal Button PresentationOpen => _open;
    internal TextBlock PresentationPath => _path;
    internal TextBlock PresentationEmpty => _empty;
    internal string PresentationDestination => _settings.Destination;
    internal IReadOnlyList<EmulationFirmwareCandidate> PresentationCandidates => _firmwares.Items.Cast<ListBoxItem>()
        .Select(item => (EmulationFirmwareCandidate)item.Tag).ToArray();
    internal string DestinationLabel(EmulationFirmwareCandidate candidate) => _settings.DestinationLabel(candidate);
    internal event EventHandler? PresentationChanged;

    internal async void RequestPresentationClose()
    {
        if (_closing || _disposed) return;
        _closing = true;
        try
        {
            _lifetime.Cancel();
            await _refreshTask;
        }
        finally { Close(); }
    }

    internal FirmwareSelectionDialog(EmulationFirmwareSettingsContent settings, Window? owner)
    {
        _settings = settings;
        Title = LocExtension.Get("Emulation.Firmware.Select");
        if (owner?.IsVisible == true) Owner = owner;
        ShowInTaskbar = false;
        WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        FlowDirection = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Width = Math.Min(860, SystemParameters.WorkArea.Width - 40);
        Height = Math.Min(480, SystemParameters.WorkArea.Height - 40);
        FontSize = 12;
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        MinWidth = 560;
        MinHeight = 320;
        SetResourceReference(BackgroundProperty, "CardBrush");
        SetResourceReference(ForegroundProperty, "TextBrush");

        _refresh = ControlUiFactory.IconTextButton(EmulationFirmwareSettingsConstants.RefreshIcon, LocExtension.Get("Common.Refresh"));
        _refresh.Click += RefreshClicked;
        _open = ControlUiFactory.IconTextButton(EmulationFirmwareSettingsConstants.OpenFolderIcon, LocExtension.Get("Emulation.Firmware.Rom.OpenFolder"));
        CompactButton(_open);
        CompactButton(_refresh);
        _open.Click += async (_, _) =>
        {
            try { await _settings.OpenFolder(); }
            catch (Exception error) { _settings.ShowError(error); }
        };
        var actions = new DockPanel { Margin = new Thickness(0, 8, 0, 10), LastChildFill = false };
        DockPanel.SetDock(_refresh, Dock.Right);
        actions.Children.Add(_refresh);
        actions.Children.Add(_open);

        _firmwares.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _firmwares.BorderThickness = new Thickness(0);
        _firmwares.SelectionChanged += SelectionChanged;
        VirtualizingPanel.SetIsVirtualizing(_firmwares, true);
        ScrollViewer.SetCanContentScroll(_firmwares, true);
        ScrollViewer.SetHorizontalScrollBarVisibility(_firmwares, ScrollBarVisibility.Disabled);
        Grid.SetIsSharedSizeScope(_firmwares, true);
        AutomationProperties.SetAutomationId(_firmwares, "FirmwareCandidates");
        _empty.Text = LocExtension.Get("Emulation.Firmware.Empty");
        var list = new Grid();
        list.Children.Add(_firmwares);
        list.Children.Add(_empty);

        _save.Content = LocExtension.Get("Common.Save");
        _save.IsEnabled = false;
        _save.Click += SaveClicked;
        AutomationProperties.SetAutomationId(_save, "SaveFirmwareSelection");
        var cancel = new Button { Content = LocExtension.Get("Common.Cancel"), IsCancel = true, MinWidth = 90, Margin = new Thickness(8, 0, 0, 0) };
        CompactButton(_save);
        CompactButton(cancel);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(_save);
        buttons.Children.Add(cancel);
        var footer = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        footer.Children.Add(_path);
        footer.Children.Add(buttons);
        var heading = new TextBlock { Text = settings.Destination, FontSize = 14, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        var root = new Grid { Margin = new Thickness(12) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(heading);
        Grid.SetRow(actions, 1); root.Children.Add(actions);
        Grid.SetRow(list, 2); root.Children.Add(list);
        Grid.SetRow(footer, 3); root.Children.Add(footer);
        Content = root;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private static void CompactButton(Button button)
    {
        button.MinHeight = 28;
        button.Padding = new Thickness(10, 3, 10, 3);
        button.FontSize = 12;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args) => await RefreshAsync();
    private async void RefreshClicked(object sender, RoutedEventArgs args) => await RefreshAsync();
    private void OnClosed(object? sender, EventArgs args) => _lifetime.Cancel();

    internal Task RefreshAsync()
    {
        if (_disposed || _lifetime.IsCancellationRequested) return Task.CompletedTask;
        return _busy ? _refreshTask : _refreshTask = RefreshCoreAsync();
    }

    private async Task RefreshCoreAsync()
    {
        _busy = true;
        _refresh.IsEnabled = false;
        UpdateSelection();
        try
        {
            var selected = (_firmwares.SelectedItem as ListBoxItem)?.Tag as EmulationFirmwareCandidate;
            var entries = await _settings.Refresh(_lifetime.Token);
            if (_lifetime.IsCancellationRequested) return;
            _firmwares.Items.Clear();
            foreach (var candidate in entries)
            {
                var item = new ListBoxItem
                {
                    Tag = candidate, Padding = new Thickness(0), Margin = new Thickness(0), FontSize = 12, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    MinHeight = 34
                };
                item.Loaded += CandidateLoaded;
                item.Unloaded += CandidateUnloaded;
                _firmwares.Items.Add(item);
                if (string.Equals(candidate.Path, selected?.Path ?? _settings.CurrentPath, StringComparison.OrdinalIgnoreCase))
                    _firmwares.SelectedItem = item;
            }
            _empty.Visibility = entries.All(candidate => string.IsNullOrEmpty(candidate.Path)) ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!_lifetime.IsCancellationRequested) _settings.ShowError(error);
        }
        finally
        {
            _busy = false;
            _refresh.IsEnabled = !_lifetime.IsCancellationRequested;
            UpdateSelection();
            PresentationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CandidateLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is not ListBoxItem { Tag: EmulationFirmwareCandidate candidate } item || item.Content is not null) return;
        item.Content = string.IsNullOrEmpty(candidate.Path)
            ? new TextBlock { Text = candidate.DisplayName, Margin = new Thickness(12, 7, 12, 7) }
            : EmulationSettingsLayout.FirmwareRow(candidate.DisplayName, candidate.Version, candidate.Compatibility, candidate.Path, _settings.DestinationLabel(candidate));
    }

    private static void CandidateUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is ListBoxItem item) item.Content = null;
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs args) => UpdateSelection();

    private void UpdateSelection()
    {
        var candidate = (_firmwares.SelectedItem as ListBoxItem)?.Tag as EmulationFirmwareCandidate;
        _path.Text = candidate is null ? string.Empty : string.Join(Environment.NewLine,
            new[] { _settings.Explanation?.Invoke(candidate), candidate.Path }.Where(value => !string.IsNullOrWhiteSpace(value)));
        _path.ToolTip = candidate?.Path;
        _save.IsEnabled = !_busy && !_lifetime.IsCancellationRequested && candidate is not null && _settings.CanUse(candidate);
    }

    private void SaveClicked(object sender, RoutedEventArgs args)
    {
        if (!_save.IsEnabled || (_firmwares.SelectedItem as ListBoxItem)?.Tag is not EmulationFirmwareCandidate candidate) return;
        SelectedFirmware = candidate;
        ThemeDialogCompletion.Complete(this, true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            Close();
            _lifetime.Cancel();
            await _refreshTask;
        }
        finally
        {
            Loaded -= OnLoaded;
            Closed -= OnClosed;
            _refresh.Click -= RefreshClicked;
            _save.Click -= SaveClicked;
            _firmwares.SelectionChanged -= SelectionChanged;
            _firmwares.Items.Clear();
            PresentationChanged = null;
            Content = null;
            Owner = null;
            _lifetime.Dispose();
        }
    }
}
