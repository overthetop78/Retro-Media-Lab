using System.Windows;
using RetroMediaLab.App.Views.Controls.Emulation.Storage;
using RetroMediaLab.App.Controllers.Emulation.Input;
using System.Windows.Controls;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Views.Controls.Emulation.Options;

internal sealed partial class EmulationModuleSettingsSection
{
    private TabControl? presentationTabs;
    private TextBlock? presentationMemoryTotal;
    internal TextBlock? PresentationMemoryTotal => presentationMemoryTotal;
    internal IReadOnlyList<EmulationMachineTab> PresentationTabs => presentationTabs?.Items.OfType<TabItem>()
        .Select(item => (EmulationMachineTab)item.Tag).ToArray() ?? [];
    internal EmulationStorageDeviceList? PresentationStorage => _storageSettings?.PresentationList;
    internal EmulationInputSettingsController? PresentationInput => _inputSettings;
    internal EmulationVideoProcessingSettingsSection PresentationVideo => _videoProcessing;
    internal EmulationCoreManagementPanel? PresentationCore => _emulatorManagement?.PresentationPanel;
    internal IEmulationModule PresentationModule => _module;
    internal EmulationMachineSettings PresentationSettings => _module.Describe(_configuration.MachineId, _configuration);
    internal IReadOnlyDictionary<string, FrameworkElement> PresentationFields => _fieldControls;
    internal ListBox PresentationMachines => _machines;
    internal EmulationMachineTab PresentationTab => _selectedTab;
    internal bool CanCreateConfiguration => !_saved.Any(configuration => configuration.MachineId == _configuration.MachineId);
    internal event EventHandler? PresentationChanged;
    internal Task RequestPresentationCommitAsync() => ExecuteUserChangeAsync();
    internal Task RequestCreateConfigurationAsync() => ExecuteAsync(SaveAsync);
    internal Task RequestPresentationTabAsync(EmulationMachineTab tab) => ExecuteAsync(() => RememberTabAndActivateAsync(tab));
}
