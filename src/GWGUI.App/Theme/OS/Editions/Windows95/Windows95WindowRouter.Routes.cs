using System.Windows;
using GWGUI.App.Constants.Localization;
using GWGUI.App.Views.Dialogs.Emulation;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using System.Windows.Threading;
using GWGUI.App.Theme.Common;
using GWGUI.App.Theme.OS.Editions.Windows95.Views;
using GWGUI.App.Views.Dialogs.Conversion;
using GWGUI.App.Views.Dialogs.Hardware;
using GWGUI.App.Views.Dialogs.Profiles;
using GWGUI.App.Views.Dialogs.Read;
using GWGUI.App.Views.Dialogs.Common;
using GWGUI.App.Views.Windows.Preferences;
using GWGUI.App.Views.Windows.EmulationPreferences;
using GWGUI.App.Views.Windows.EmulationModuleOptions;
using GWGUI.App.Views.Windows.About;
using GWGUI.App.Views.Windows.Logs;
using GWGUI.App.Views.Windows.Tools;
using GWGUI.App.Views.Windows.Conversion;
using GWGUI.App.Views.Windows.Updates;
using GWGUI.App.Views.Dialogs.Explorer;

namespace GWGUI.App.Theme.OS.Editions.Windows95;

internal sealed partial class Windows95WindowRouter
{
    private sealed record WindowRoute(string Title, Size Size, Func<FrameworkElement> Create, Action RequestClose);
    private static readonly Size EmulationPreferencesSize = new(940, 660);
    private static readonly Size CoreOptionsSize = new(760, 500);
    private static readonly Size AddStorageDeviceSize = new(500, 190);
    private static readonly Size ManagedStorageSize = new(760, 520);
    private static readonly Size FloppySettingsSize = new(760, 370);
    private static readonly Size HardDiskSettingsSize = new(900, 650);
    private static readonly Size CompactDiscSettingsSize = new(700, 390);
    private static readonly Size CartridgeSettingsSize = new(640, 300);
    private static readonly Size MemoryCardDirectorySize = new(760, 230);
    private static readonly Size MediaCreationSize = new(780, 350);
    private static readonly Size FirmwareSelectionSize = new(900, 440);
    private static readonly Size PreferencesSize = new(900, 620);
    private static readonly Size ProfileNameSize = new(460, 170);
    private static readonly Size ReadConflictSize = new(620, 180);
    private static readonly Size ConversionConflictSize = new(740, 430);
    private static readonly Size MissingHardwareSize = new(640, 350);
    private static readonly Size FileSelectionSize = new(660, 360);
    private static readonly Size FolderSelectionSize = new(540, 360);
    private static readonly Size ModuleSettingsSize = new(960, 660);
    private static readonly Size DetailedErrorSize = new(620, 420);
        private static readonly Size AboutSize = new(560, 430);
    private static readonly Size LogHistorySize = new(900, 540);
    private static readonly Size UpdatesSize = new(980, 650);
    private static readonly Size FileMigrationSize = new(860, 580);
    private static readonly Size ExplorerIssuesSize = new(740, 440);
    private static readonly Size AdvancedToolSize = new(820, 620);

    private WindowRoute? Route(Window source)
    {
        switch (source)
        {
            case UpdatesWindow updates:
                return new WindowRoute("Updates.Title", UpdatesSize,
                    () => new UpdatesView(updates.Section, updates.FooterButton, updates.FooterHint, updates.RequestPresentationClose),
                    updates.RequestPresentationClose);
            case FileMigrationWindow migration:
                return new WindowRoute("Migration.Title", FileMigrationSize,
                    () => new FileMigrationView(migration), migration.RequestPresentationClose);
            case ExplorerIssuesWindow issues:
                return new WindowRoute("Explorer.Warnings", ExplorerIssuesSize,
                    () => new ExplorerIssuesView(issues), issues.Close);
            case GwToolWindow tool:
                return new WindowRoute("Common.Command", AdvancedToolSize,
                    () => new ToolView(tool), tool.RequestPresentationClose);
            case LogHistoryWindow logs:
                return new WindowRoute("Logs.History", LogHistorySize,
                    () => new LogHistoryView(logs), logs.RequestPresentationClose);
            case CommonErrorDialog error:
                return new WindowRoute("Error.Title", DetailedErrorSize,
                    () => new ErrorView(error), error.Close);
            case AboutWindow about:
                return new WindowRoute("Menu.About", AboutSize,
                    () => new AboutView(about), about.Close);
            case EmulationModuleOptionsWindow module:
                return new WindowRoute("Options.EmulationTitle", ModuleSettingsSize,
                    () => new ModuleSettingsView(module.PresentationSection, module.RequestPresentationClose, module.InitializePresentationAsync),
                    module.RequestPresentationClose);
            case FileSelectionDialog files:
                return new WindowRoute("FileDialog.Open",
                    files.Kind == GWGUI.App.Enums.Services.Dialogs.FileSelectionKind.Folder ? FolderSelectionSize : FileSelectionSize,
                    () => new FileSelectionView(files), files.RequestPresentationClose);
            case PreferencesWindow preferences:
                return new WindowRoute("Options.PreferencesTitle", PreferencesSize,
                    () => new PreferencesView(preferences.PresentationNavigation, preferences.RequestPresentationClose), preferences.RequestPresentationClose);
            case EmulationPreferencesWindow emulation:
                return new WindowRoute("Options.EmulationTitle", EmulationPreferencesSize,
                    () => new EmulationPreferencesView(emulation.PresentationSection, emulation.RequestPresentationClose), emulation.RequestPresentationClose);
            case FirmwareSelectionDialog firmware:
                return new WindowRoute("Emulation.Firmware.Select", FirmwareSelectionSize,
                    () => new FirmwareSelectionView(firmware), firmware.RequestPresentationClose);
            case CoreOptionsDialog core:
                return new WindowRoute("Emulation.Core.NameOptions", CoreOptionsSize,
                    () => new CoreOptionsView(core), core.Close);
            case AddStorageDeviceDialog add:
                return new WindowRoute("Emulation.Storage.Device.Add", AddStorageDeviceSize,
                    () => new AddStorageDeviceView(add), add.Close);
            case ManagedStorageConfigurationDialog managed:
                return new WindowRoute(EmulationResourceKeys.StorageDeviceConfigure, ManagedStorageSize,
                    () => new ManagedStorageView(managed), managed.Close);
            case FloppyDriveConfigurationDialog floppy:
                return new WindowRoute(EmulationResourceKeys.StorageDeviceConfigure, FloppySettingsSize,
                    () => new FloppySettingsView(floppy), floppy.Close);
            case HardDiskDriveConfigurationDialog disk:
                return new WindowRoute(EmulationResourceKeys.StorageDeviceConfigure, HardDiskSettingsSize,
                    () => new HardDiskSettingsView(disk), disk.RequestPresentationClose);
            case CompactDiscDriveConfigurationDialog disc:
                return new WindowRoute(EmulationResourceKeys.StorageDeviceConfigure, CompactDiscSettingsSize,
                    () => new CompactDiscSettingsView(disc), disc.Close);
            case CartridgeSlotConfigurationDialog cartridge:
                return new WindowRoute(EmulationResourceKeys.StorageDeviceConfigure, CartridgeSettingsSize,
                    () => new CartridgeSettingsView(cartridge), cartridge.Close);
            case MemoryCardDirectoryConfigurationDialog directory:
                return new WindowRoute(EmulationResourceKeys.StorageDeviceConfigure, MemoryCardDirectorySize,
                    () => new MemoryCardDirectoryView(directory), directory.Close);
            case MediaCreationDialog creation:
                return new WindowRoute("Common.Create", MediaCreationSize,
                    () => new MediaCreationView(creation), creation.RequestPresentationClose);
            case MessageDialog message:
                return new WindowRoute("Common.OK", MessageView.PreferredSize(message, context),
                    () => new MessageView(message), message.Close);
            case ProfileNameWindow name:
                return new WindowRoute("Profile.SaveTitle", ProfileNameSize,
                    () => new ProfileNameView(name), name.Close);
            case ReadConflictWindow read:
                return new WindowRoute("Read.FileExistsTitle", ReadConflictSize,
                    () => new ReadConflictView(read), read.Close);
            case ConversionConflictWindow conversion:
                return new WindowRoute("Conflict.Title", ConversionConflictSize,
                    () => new ConversionConflictView(conversion), conversion.Close);
            case HardwareUnavailableWindow hardware:
                return new WindowRoute("Hardware.StartupTitle", MissingHardwareSize,
                    () => new HardwareUnavailableView(hardware), hardware.Close);
            default: return null;
        }
    }
}
