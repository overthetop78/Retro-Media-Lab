using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Contracts.Emulation.Machine;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Controllers.Emulation.Options;
using RetroMediaLab.App.Functions.Views.Emulation.Settings;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Services.Emulation;
using RetroMediaLab.App.Constants.Views.Emulation;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationModuleSettingsSection
{
    private async void MachineChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_loading || _machines.SelectedItem is not EmulationMachineChoice selected) return;
        await ExecuteAsync(async () =>
        {
            CaptureEditorValues();
            await _session.SaveBeforeSwitchingAsync(_configuration, NotifyConfigurationSaved);
            _configuration = _session.SelectForEditing(selected.Definition.Id);
            RebuildEditor();
            NotifyEditingContextChanged();
        });
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception error) { _showError(error); }
    }

    private Task ExecuteUserChangeAsync() => ExecuteAsync(ApplyUserChangeAsync);

    private async Task ApplyUserChangeAsync()
    {
        if (_inputSettings?.HasErrors == true) return;
        _session.GetVideoProfile(_configuration.Id);
        CaptureEditorValues();
        await _session.PersistAsync(_configuration, NotifyConfigurationSaved);
    }

    private void NotifyConfigurationSaved(IEmulationConfiguration configuration) =>
        ConfigurationSaved?.Invoke(this, new EmulationConfigurationSavedEventArgs(configuration));

    private async Task SaveAsync()
    {
        if (_inputSettings?.HasErrors == true) return;
        _session.GetVideoProfile(_configuration.Id);
        ApplyEditorValues(includeEmptyFields: true);
        if (_emulatorManagement is not null)
            await _emulatorManagement.EnsureEmulatorSelectedAsync();
        _configuration = _session.ApplyVideoRenderer(_configuration);
        var configuration = _configuration;
        await _session.SaveAsync(configuration);
        NotifyConfigurationSaved(configuration);
        await ReloadAsync();
    }

    private Task SaveVideoProfileAsync() => _session.SaveVideoProfileAsync(_configuration.Id);

    private static string? ReadValue(FrameworkElement control) => control switch
    {
        RetroMediaLab.App.Views.Controls.Common.ConstrainedTextBox input => input.CommittedValue,
        Button { Tag: string directory } => directory,
        TextBox text => text.Text,
        Grid { Tag: TextBox path } => path.Text,
        _ => RetroMediaLab.App.Functions.Views.Emulation.Settings.EmulationSettingsFieldControlFunctions.Read(control)
    };

    private void SelectMachine(string machineId)
    {
        _loading = true;
        _machines.SelectedItem = _machines.Items.Cast<EmulationMachineChoice>().First(item => item.Definition.Id == machineId);
        _loading = false;
    }

    private void RebuildEditor()
    {
        EmulationSettingsLayout.DetachReusableElement(_machines);
        EmulationSettingsLayout.DetachReusableElement(_videoProcessing);
        var rebuilt = BuildEditor();
        Content = rebuilt;
        PresentationChanged?.Invoke(this, EventArgs.Empty);
        if (_emulatorManagement is not null)
            _ = ExecuteAsync(_emulatorManagement.RefreshAsync);
        if (HasFirmwareFields(_selectedTab) && _firmwareManagement is not null)
            _ = ExecuteAsync(_firmwareManagement.RefreshAsync);
    }

    private void SetConfiguration(IEmulationConfiguration configuration)
    {
        _configuration = configuration;
        RebuildEditor();
    }

    private void NotifyEditingContextChanged()
    {
        var machine = (_machines.SelectedItem as EmulationMachineChoice)?.DisplayName ?? _configuration.MachineId;
        EditingContextChanged?.Invoke(this, new EmulationMachineEditingContext(LocExtension.GetForModule(_module, _module.DisplayResourceKey), machine));
    }

    private Task TabActivatedAsync(EmulationMachineTab tab) => RememberTabAndActivateAsync(tab);

    private async Task RememberTabAndActivateAsync(EmulationMachineTab tab)
    {
        _selectedTab = tab;
        if (HasFirmwareFields(tab) && _firmwareManagement is not null)
            await _firmwareManagement.RefreshAsync();
    }

    internal void RefreshLocalizedContent()
    {
        CaptureEditorValues();
        var choices = _module.Machines.Select(CreateMachineChoice).ToArray();
        _machines.ItemsSource = choices;
        SelectMachine(_configuration.MachineId);
        FlowDirection = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        RebuildEditor();
        NotifyEditingContextChanged();
    }

    private void CaptureEditorValues()
    {
        ApplyEditorValues(includeEmptyFields: false);
        _configuration = _session.ApplyVideoRenderer(_configuration);
    }

    private void ApplyEditorValues(bool includeEmptyFields)
    {
        var values = includeEmptyFields || _fieldControls.Count != 0
            ? _fieldControls.ToDictionary(item => item.Key, item => ReadValue(item.Value), StringComparer.Ordinal)
            : null;
        EmulationConfigurationPersistenceFunctions.ApplyEditorValues(_module, ref _configuration, values,
            _inputSettings is null ? null : _inputSettings.Apply,
            _storageSettings is null ? null : _storageSettings.Apply);
    }
}
