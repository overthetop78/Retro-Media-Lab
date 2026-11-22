using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.App.Constants.Emulation.Errors;
using RetroMediaLab.App.Presenters.Common;
using RetroMediaLab.App.Contracts.Emulation.Configurations;
using RetroMediaLab.App.Localization.Extensions;
using System.Windows;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

public sealed partial class EmulationPreferencesSection
{
    private Task ReloadConfigurationsAsync() => _configurationCatalog.ReloadAsync();

    private async Task DeleteConfigurationAsync(EmulationConfigurationTableRow row)
    {
        var answer = WpfMessageDialogService.ShowMessage(
            string.Format(LocExtension.Get("Emulation.Configuration.DeleteConfirm"), LocExtension.GetForModule(row.Module, row.Module.DisplayResourceKey), row.MachineName),
            LocExtension.Get("Common.Delete"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            await _configurationCatalog.DeleteAsync(row);
        }
        catch (Exception error)
        {
            ControlErrorPresenter.ShowEmulation(this, error, ControlErrorContexts.EmulationConfigurationManagement, LocExtension.GetForModule(row.Module, row.Module.DisplayResourceKey));
        }
    }

    private async Task EditConfigurationAsync(EmulationConfigurationTableRow row)
    {
        if (EditConfigurationRequested is { } edit)
            await edit(row.Module, row.Configuration);
    }

    private void RefreshConfigurationControls()
    {
        _configurationSelector.SetItems(_configurationCatalog.ConfiguredModules, SelectedModule,
            _configurationCatalog.VisibleMachines, SelectedMachineId);
        _configurationTable.SetRows(_configurationCatalog.VisibleRows);
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

}
