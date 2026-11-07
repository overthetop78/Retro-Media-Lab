using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Contracts.Emulation.Storage;
using RetroMediaLab.App.Controllers.Emulation.Storage;
using RetroMediaLab.App.Theme.OS.Editions.Windows31.Views;
using RetroMediaLab.App.Views.Controls.Emulation.Storage;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class Windows31ModuleStorageTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task CommandsKeepTheirCapabilitiesAndSelectionFollowsTheUpdatedSlots() => sta.Run(() =>
    {
        var source = new EmulationStorageDeviceList();
        var permanent = new EmulationStorageDeviceItem(EmulationMediaSlot.Tape0, "Tape", EmulationMediaType.Tape,
            "Model", null, CanRemove: false, CanConfigure: true, SupportDisplayName: "Memory");
        var optional = new EmulationStorageDeviceItem(EmulationMediaSlot.Floppy0, "Floppy", EmulationMediaType.Floppy,
            "Model", null, CanConfigure: false, SupportDisplayName: "Memory");
        source.SetDevices([permanent, optional]);
        var configured = new List<EmulationStorageDeviceItem>();
        var removed = new List<EmulationStorageDeviceItem>();
        var added = 0;
        source.ConfigureRequested += (_, args) => configured.Add(args.Device);
        source.RemoveRequested += (_, args) => removed.Add(args.Device);
        source.AddRequested += (_, _) => added++;
        using var view = new ModuleStorageView(source);
        var devices = (ListBox)view.FindName("Devices");
        var configure = (Button)view.FindName("Configure");
        var remove = (Button)view.FindName("Remove");
        Assert.Equal(2, devices.Items.Count);
        Assert.True(configure.IsEnabled);
        Assert.False(remove.IsEnabled);
        remove.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Empty(removed);
        configure.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Same(permanent, Assert.Single(configured));
        devices.SelectedIndex = 1;
        Assert.False(configure.IsEnabled);
        Assert.True(remove.IsEnabled);
        configure.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Single(configured);
        source.SetDevices([permanent, optional with { Model = "Changed" }]);
        Assert.Equal(EmulationMediaSlot.Floppy0, ((EmulationStorageDeviceList.PresentationRow)devices.SelectedItem).Device.Slot);
        remove.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal("Changed", Assert.Single(removed).Model);
        source.SetCanAdd(false);
        Assert.Equal(Visibility.Collapsed, ((Button)view.FindName("Add")).Visibility);
        source.SetCanAdd(true);
        ((Button)view.FindName("Add")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, added);
        view.Dispose();
        source.SetDevices([]);
        configure.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Single(configured);
        Assert.Null(devices.ItemsSource);
    });

    [Fact]
    public Task RealControllerKeepsPermanentDevicesAndAppliesTheRemainingMedia() => sta.Run(() =>
    {
        var configuration = new Windows31EmulationViewTests.Configuration("storage-view", Guid.NewGuid(), "machine");
        var tape = new EmulationMediaDevice(EmulationMediaSlot.Tape0, EmulationMediaType.Tape, [],
            IsPermanent: true, ConfigurationKind: EmulationStorageConfigurationKind.ManagedStorage);
        var floppy = new EmulationMediaDevice(EmulationMediaSlot.Floppy0, EmulationMediaType.Floppy, []);
        var media = new EmulationMedia("memory-tape", EmulationMediaSlot.Tape0, EmulationMediaType.Tape, false, true);
        var settings = new EmulationStorageSettings([tape, floppy], [tape.Slot, floppy.Slot], [media]);
        EmulationStorageSettings? applied = null;
        var manager = ControlledDependencies.Simulate<IEmulationStorageSettingsManager>((method, args) => method.Name switch
        {
            "DescribeStorageSettings" => settings,
            "ApplyStorageSettings" => Apply((IEmulationConfiguration)args[0]!, (EmulationStorageSettings)args[1]!),
            _ => throw new InvalidOperationException(method.Name)
        });
        IEmulationConfiguration Apply(IEmulationConfiguration original, EmulationStorageSettings result) { applied = result; return original; }
        var controller = new EmulationStorageSettingsController(configuration.ModuleId, manager, _ => throw new InvalidOperationException());
        var source = (EmulationStorageDeviceList)controller.CreateContent(configuration);
        using var view = new ModuleStorageView(source);
        var devices = (ListBox)view.FindName("Devices");
        Assert.False(((Button)view.FindName("Remove")).IsEnabled);
        source.RequestPresentationRemove(source.PresentationRows[0].Device);
        Assert.Equal(2, devices.Items.Count);
        devices.SelectedIndex = 1;
        ((Button)view.FindName("Remove")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Single(devices.Items);
        controller.Apply(configuration);
        Assert.Equal(tape.Slot, Assert.Single(applied!.ConfiguredSlots));
        Assert.Same(media, Assert.Single(applied.MountedMedia));
        Assert.Equal(Visibility.Visible, ((Button)view.FindName("Add")).Visibility);
    });
}
