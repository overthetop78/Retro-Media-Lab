using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.App.Functions.Views.Emulation.Storage;
using RetroMediaLab.App.Localization.Extensions;
using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Views.Dialogs.Emulation.Storage;

public sealed class AddStorageDeviceDialog : Window
{
    private readonly ComboBox _type = new();
    internal ComboBox PresentationType => _type;

    public EmulationMediaType SelectedType => _type.SelectedItem is StorageDeviceChoice choice ? choice.Type : EmulationMediaType.Floppy;

    public AddStorageDeviceDialog(IEnumerable<EmulationMediaType> availableTypes)
    {
        Title = LocExtension.Get(EmulationResourceKeys.StorageDeviceAdd);
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        MinWidth = 460;
        _type.ItemsSource = availableTypes.Select(type => new StorageDeviceChoice(type, DeviceLabel(type))).ToArray();
        _type.SelectedIndex = 0;
        Content = StorageDialogUi.DialogLayout(
            StorageDialogUi.Card(LocExtension.Get(EmulationResourceKeys.DeviceType), StorageDialogUi.Field(LocExtension.Get(EmulationResourceKeys.DeviceType), _type)),
            StorageDialogUi.Footer(this, LocExtension.Get(EmulationResourceKeys.StorageDeviceAdd)));
    }

    private static string DeviceLabel(EmulationMediaType type) => type switch
    {
        EmulationMediaType.Floppy => LocExtension.Get(EmulationResourceKeys.FloppyDevice),
        EmulationMediaType.HardDisk => LocExtension.Get(EmulationResourceKeys.HardDiskDevice),
        EmulationMediaType.CompactDisc => LocExtension.Get(EmulationResourceKeys.CompactDiscDevice),
        EmulationMediaType.Cassette => LocExtension.Get(EmulationResourceKeys.CassetteDevice),
        EmulationMediaType.Cartridge => LocExtension.Get(EmulationResourceKeys.CartridgeDevice),
        EmulationMediaType.MemoryCard => LocExtension.Get(EmulationResourceKeys.MemoryCardDevice),
        _ => type.ToString()
    };
}
