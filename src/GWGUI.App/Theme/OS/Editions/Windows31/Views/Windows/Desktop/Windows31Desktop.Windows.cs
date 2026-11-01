using System.Windows;
using GWGUI.App.Constants.Localization;
using GWGUI.App.Views.Dialogs.Emulation;
using GWGUI.App.Views.Dialogs.Emulation.Storage;
using System.Windows.Threading;
using GWGUI.App.Theme.Common;
using GWGUI.App.Theme.OS.Editions.Windows31.Views;
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

namespace GWGUI.App.Theme.OS.Editions.Windows31;

internal sealed partial class Windows31Desktop
{
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
    private static readonly Size DetailedErrorSize = new(620, 160);
    private const double DetailedErrorMaximumHeight = 460;
    private static readonly Size AboutSize = new(560, 430);
    private static readonly Size LogHistorySize = new(900, 540);
    private static readonly Size UpdatesSize = new(980, 650);
    private static readonly Size FileMigrationSize = new(860, 580);
    private static readonly Size ExplorerIssuesSize = new(740, 440);
    private static readonly Size AdvancedToolSize = new(820, 620);
    private readonly Dictionary<Window, Windows31DialogKind> presentedWindows = [];

    internal bool TryShowWindow(Window source)
    {
        switch (source)
        {
            case UpdatesWindow updates:
                ShowFunctionalWindow(updates, Windows31DialogKind.Updates, "Updates.Title", UpdatesSize,
                    () => new UpdatesView(updates.Section, updates.FooterButton, updates.FooterHint, updates.RequestPresentationClose),
                    updates.RequestPresentationClose);
                break;
            case FileMigrationWindow migration:
                ShowFunctionalWindow(migration, Windows31DialogKind.FileMigration, "Migration.Title", FileMigrationSize,
                    () => new FileMigrationView(migration), migration.RequestPresentationClose);
                break;
            case ExplorerIssuesWindow issues:
                ShowFunctionalWindow(issues, Windows31DialogKind.ExplorerIssues, "Explorer.Warnings", ExplorerIssuesSize,
                    () => new ExplorerIssuesView(issues), issues.Close);
                break;
            case GwToolWindow tool:
                ShowFunctionalWindow(tool, Windows31DialogKind.AdvancedTool, "Common.Command", AdvancedToolSize,
                    () => new ToolView(tool), tool.RequestPresentationClose);
                break;
            case LogHistoryWindow logs:
                ShowFunctionalWindow(logs, Windows31DialogKind.LogHistory, "Logs.History", LogHistorySize,
                    () => new LogHistoryView(logs), logs.RequestPresentationClose);
                break;
            case CommonErrorDialog error:
                ShowFunctionalWindow(error, Windows31DialogKind.DetailedError, "Error.Title", DetailedErrorSize,
                    () => new ErrorView(error), error.Close);
                break;
            case AboutWindow about:
                ShowFunctionalWindow(about, Windows31DialogKind.About, "Menu.About", AboutSize,
                    () => new AboutView(about), about.Close);
                break;
            case EmulationModuleOptionsWindow module:
                ShowFunctionalWindow(module, Windows31DialogKind.ModuleSettings, "Options.EmulationTitle", ModuleSettingsSize,
                    () => new ModuleSettingsView(module.PresentationSection, module.RequestPresentationClose, module.InitializePresentationAsync),
                    module.RequestPresentationClose);
                break;
            case FileSelectionDialog files:
                ShowFunctionalWindow(files, Windows31DialogKind.FileSelection, "FileDialog.Open",
                    files.Kind == GWGUI.App.Enums.Services.Dialogs.FileSelectionKind.Folder ? FolderSelectionSize : FileSelectionSize,
                    () => new FileSelectionView(files), files.RequestPresentationClose);
                break;
            case PreferencesWindow preferences:
                ShowPreferences(preferences);
                break;
            case EmulationPreferencesWindow emulation:
                ShowFunctionalWindow(emulation, Windows31DialogKind.EmulationPreferences, "Options.EmulationTitle", EmulationPreferencesSize,
                    () => new EmulationPreferencesView(emulation.PresentationSection, emulation.RequestPresentationClose), emulation.RequestPresentationClose);
                break;
            case FirmwareSelectionDialog firmware:
                ShowFunctionalWindow(firmware, Windows31DialogKind.FirmwareSelection, "Emulation.Firmware.Select", FirmwareSelectionSize,
                    () => new FirmwareSelectionView(firmware), firmware.RequestPresentationClose);
                break;
            case CoreOptionsDialog core:
                ShowFunctionalWindow(core, Windows31DialogKind.CoreOptions, "Emulation.Core.NameOptions", CoreOptionsSize,
                    () => new CoreOptionsView(core), core.Close);
                break;
            case AddStorageDeviceDialog add:
                ShowFunctionalWindow(add, Windows31DialogKind.AddStorageDevice, "Emulation.Storage.Device.Add", AddStorageDeviceSize,
                    () => new AddStorageDeviceView(add), add.Close);
                break;
            case ManagedStorageConfigurationDialog managed:
                ShowFunctionalWindow(managed, Windows31DialogKind.ManagedStorage, EmulationResourceKeys.StorageDeviceConfigure, ManagedStorageSize,
                    () => new ManagedStorageView(managed), managed.Close);
                break;
            case FloppyDriveConfigurationDialog floppy:
                ShowFunctionalWindow(floppy, Windows31DialogKind.FloppySettings, EmulationResourceKeys.StorageDeviceConfigure, FloppySettingsSize,
                    () => new FloppySettingsView(floppy), floppy.Close);
                break;
            case HardDiskDriveConfigurationDialog disk:
                ShowFunctionalWindow(disk, Windows31DialogKind.HardDiskSettings, EmulationResourceKeys.StorageDeviceConfigure, HardDiskSettingsSize,
                    () => new HardDiskSettingsView(disk), disk.RequestPresentationClose);
                break;
            case CompactDiscDriveConfigurationDialog disc:
                ShowFunctionalWindow(disc, Windows31DialogKind.CompactDiscSettings, EmulationResourceKeys.StorageDeviceConfigure, CompactDiscSettingsSize,
                    () => new CompactDiscSettingsView(disc), disc.Close);
                break;
            case CartridgeSlotConfigurationDialog cartridge:
                ShowFunctionalWindow(cartridge, Windows31DialogKind.CartridgeSettings, EmulationResourceKeys.StorageDeviceConfigure, CartridgeSettingsSize,
                    () => new CartridgeSettingsView(cartridge), cartridge.Close);
                break;
            case MemoryCardDirectoryConfigurationDialog directory:
                ShowFunctionalWindow(directory, Windows31DialogKind.MemoryCardDirectory, EmulationResourceKeys.StorageDeviceConfigure, MemoryCardDirectorySize,
                    () => new MemoryCardDirectoryView(directory), directory.Close);
                break;
            case MediaCreationDialog creation:
                ShowFunctionalWindow(creation, Windows31DialogKind.MediaCreation, "Common.Create", MediaCreationSize,
                    () => new MediaCreationView(creation), creation.RequestPresentationClose);
                break;
            case MessageDialog message:
                ShowFunctionalWindow(message, Windows31DialogKind.Message, "Common.OK", MessageView.PreferredSize(message, this),
                    () => new MessageView(message), message.Close);
                break;
            case ProfileNameWindow name:
                ShowFunctionalWindow(name, Windows31DialogKind.ProfileName, "Profile.SaveTitle", ProfileNameSize,
                    () => new ProfileNameView(name), name.Close);
                break;
            case ReadConflictWindow read:
                ShowFunctionalWindow(read, Windows31DialogKind.ReadConflict, "Read.FileExistsTitle", ReadConflictSize,
                    () => new ReadConflictView(read), read.Close);
                break;
            case ConversionConflictWindow conversion:
                ShowFunctionalWindow(conversion, Windows31DialogKind.ConversionConflict, "Conflict.Title", ConversionConflictSize,
                    () => new ConversionConflictView(conversion), conversion.Close);
                break;
            case HardwareUnavailableWindow hardware:
                ShowFunctionalWindow(hardware, Windows31DialogKind.MissingHardware, "Hardware.StartupTitle", MissingHardwareSize,
                    () => new HardwareUnavailableView(hardware), hardware.Close);
                break;
            default: return false;
        }
        return true;
    }

    internal void ShowPreferences(PreferencesWindow source) => ShowFunctionalWindow(source,
        Windows31DialogKind.Preferences, "Options.PreferencesTitle", PreferencesSize,
        () => new PreferencesView(source.PresentationNavigation, source.RequestPresentationClose), source.RequestPresentationClose);

    // The source window retains its functional ownership and is never displayed.
    private void ShowFunctionalWindow(Window source, Windows31DialogKind kind, string titleResource,
        Size size, Func<FrameworkElement> create, Action requestClose)
    {
        var loop = new DispatcherFrame();
        var closed = false;
        var created = false;
        var owner = Windows31ApplicationId.ProgramManager;
        var sourceOwner = ThemeDialogCompletion.Owner(source) ?? source.Owner;
        var activeParent = dialogs.Values.LastOrDefault(dialog => dialog.Placement.IsActive && dialog.Mode == Windows31DialogMode.Modal);
        var parent = sourceOwner is not null && presentedWindows.TryGetValue(sourceOwner, out var ownerKind)
            ? (Windows31DialogKind?)ownerKind : activeParent?.Kind;
        var parentInstance = sourceOwner is not null && presentedWindows.ContainsKey(sourceOwner)
            ? sourceOwner : activeParent?.Instance;
        EventHandler sourceClosed = (_, _) =>
        {
            closed = true;
            loop.Continue = false;
            if (created) CloseDialog(owner, kind, source);
        };
        source.Closed += sourceClosed;
        using var completion = ThemeDialogCompletion.Attach(source, _ => { if (!closed) requestClose(); });
        using var presentation = new ThemeBindingScope();
        try
        {
            source.Owner ??= Window.GetWindow(this);
            presentedWindows.Add(source, kind);
            ShowDialog(owner, kind, Windows31DialogMode.Modal, titleResource, size,
                _ =>
                {
                    var view = create();
                    created = true;
                    return new Windows31Document(view, new WindowLifetime((IDisposable)view, () => { if (!closed) requestClose(); }));
                }, () => { if (!closed) requestClose(); }, parent, source, parentInstance);
            if (created && dialogs.TryGetValue((owner, kind, source), out var dialogEntry))
            {
                if (!string.IsNullOrWhiteSpace(source.Title))
                    presentation.OneWay(source, Window.TitleProperty, dialogEntry.Frame, Windows31Frame.TitleProperty);
                dialogEntry.Frame.AllowResize = source.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
                if (dialogEntry.Document.View is ErrorView errorView)
                    errorView.FitHeight(dialogEntry.Frame, dialogEntry.Placement, DetailedErrorMaximumHeight);
                if (source is MessageDialog message)
                {
                    dialogEntry.Frame.AllowClose = message.CancelResult != MessageBoxResult.None;
                    var messageView = (MessageView)dialogEntry.Document.View;
                    messageView.FitSize(dialogEntry.Frame, dialogEntry.Placement);
                    messageView.ActivateDefault();
                }
            }
            if (!created && !closed) requestClose();
            if (!closed) Dispatcher.PushFrame(loop);
        }
        finally
        {
            try
            {
                if (!closed)
                {
                    requestClose();
                    if (!closed && !source.Dispatcher.HasShutdownStarted) Dispatcher.PushFrame(loop);
                }
            }
            finally
            {
                source.Closed -= sourceClosed;
                presentedWindows.Remove(source);
                if (created) CloseDialog(owner, kind, source);
            }
        }
    }

    private sealed class WindowLifetime(IDisposable view, Action close) : IDisposable
    {
        public void Dispose()
        {
            try { view.Dispose(); }
            finally { close(); }
        }
    }
}
