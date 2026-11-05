using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Enums;
using System.Text.Json;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Configuration Winchester et formats proposés ; les commandes sont testées sur la cible physique ACB-4000.</summary>
public sealed class AcornEmuScsiTests
{
    [Fact]
    public void StorageFieldsUseHardwareSoundAndCompactControlsWithoutAProfileSelector()
    {
        var factory = new AcornEmuMachineFactory();
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var devices = factory.DescribeStorage(configuration).AvailableDevices;
        foreach (var device in devices.Where(item => item.Slot.Category is EmulationMediaCategory.HardDisk or EmulationMediaCategory.FloppyDrive))
        {
            var fields = device.ConfigurationFields!;
            Assert.DoesNotContain(fields, field => field.Id == StorageConstants.SoundProfileOption);
            var volume = Assert.Single(fields, field => field.Id == StorageConstants.SoundVolumeOption);
            Assert.True(volume.NumericRange!.IsVertical);
            if (device.Slot.Category == EmulationMediaCategory.HardDisk)
            {
                Assert.True(Assert.Single(fields, field => field.Id == StorageConstants.SpeedOption).UseRadioButtons);
                Assert.DoesNotContain(fields, field => field.Id is StorageConstants.ReadLatencyOption or StorageConstants.WriteLatencyOption);
            }
            else Assert.NotEmpty(device.FloppyOptions!.SpeedChoices!);
        }
    }

    [Fact]
    public void WinchesterProfilesKeepExactNativeGeometriesAndAnAddressBoundedCustomProfile()
    {
        var formats = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.HardDiskFormats.All;
        var native = formats.Where(format => format.FixedGeometry is not null).ToArray();
        Assert.Equal(4, native.Length);
        foreach (var format in native)
        {
            Assert.Equal(format.FixedGeometry!.Capacity, format.DefaultBytes);
            Assert.True(format.SupportsFixedAllocation);
            Assert.Equal(format.DefaultBytes, Assert.Single(format.AllowedCapacitiesBytes!));
            Assert.True(RetroMediaLab.Emulation.HardDisks.HardDiskImageValidation.IsSupportedCapacity(format.DefaultBytes, format));
            Assert.False(RetroMediaLab.Emulation.HardDisks.HardDiskImageValidation.IsSupportedCapacity(format.DefaultBytes + 256, format));
        }
        foreach (var custom in formats.Where(format => format.FixedGeometry is null))
        {
            Assert.Null(custom.AllowedCapacitiesBytes);
            Assert.Equal(WinchesterModelConstants.MaximumBytes, custom.MaximumBytes);
            Assert.True(RetroMediaLab.Emulation.HardDisks.HardDiskImageValidation.IsSupportedCapacity(custom.DefaultBytes, custom));
            Assert.False(RetroMediaLab.Emulation.HardDisks.HardDiskImageValidation.IsSupportedCapacity(custom.MaximumBytes + 256, custom));
            Assert.False(RetroMediaLab.Emulation.HardDisks.HardDiskImageValidation.IsSupportedCapacity(custom.DefaultBytes + 1, custom));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EachWinchesterUnitUsesTheCommonConfigurationAndMediaContracts(int unit)
    {
        var factory = new AcornEmuMachineFactory(); var empty = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var available = factory.DescribeStorage(empty);
        Assert.Empty(available.ConfiguredSlots); Assert.Equal(WinchesterDriveConstants.DriveCount, available.AvailableDevices.Count(device => device.Slot.Category == EmulationMediaCategory.HardDisk));
        var slot = new EmulationMediaSlot(EmulationMediaCategory.HardDisk, unit);
        var settings = available with { ConfiguredSlots = [slot], MountedMedia = [new("unit.dat", slot, EmulationMediaType.HardDisk, false, true)], DeviceSettings = [new(slot, InterfaceId: "scsi", Options: new Dictionary<string, string> { ["protected"] = bool.TrueString, ["speed"] = "200", ["readLatency"] = "3", ["writeLatency"] = "7" })] };
        var configuration = factory.ApplyStorage(empty, settings);
        var restored = JsonSerializer.Deserialize<MachineConfiguration>(JsonSerializer.Serialize(configuration))!;
        var stored = factory.DescribeStorage(restored); Assert.Equal(slot, Assert.Single(stored.ConfiguredSlots));
        Assert.Equal(MediaCategory.HardDisk, Assert.Single(restored.Media).Category);
        Assert.Equal(EmulationMediaType.HardDisk, Assert.Single(factory.ResolveConfiguredMedia(restored)).Type);
        var device = Assert.Single(factory.DescribeMediaDevices(restored)); Assert.Equal(slot, device.Slot); Assert.False(device.IsRemovable);
        Assert.Equal(6, device.HardDiskFormats!.Count);
        Assert.All(device.HardDiskFormats, format => { Assert.True(format.SupportsExplicitGeometry); Assert.NotNull(format.ImageCreator); });
        var saved = Assert.Single(factory.DescribeDeviceSettings(restored)); Assert.Equal("200", saved.Options![StorageConstants.SpeedOption]);
        Assert.False(saved.Options.ContainsKey(StorageConstants.ReadLatencyOption));
        Assert.False(saved.Options.ContainsKey(StorageConstants.WriteLatencyOption));
        Assert.Contains(factory.GetFirmwareSlots(restored), firmware => firmware.FieldId == FloppyExtensionConstants.IntelRom && firmware.IsRequired);
        Assert.True(factory.RequiresOptionRestart(restored, StorageConstants.HardDiskKey(unit, "speed")));
        var activity = factory.MediaActivity(restored, new Dictionary<int, bool> { [StorageConstants.HardDiskLedStart + unit] = true }); Assert.True(activity[slot]);
        var removed = factory.ApplyStorage(restored, stored with { ConfiguredSlots = [], MountedMedia = [], DeviceSettings = [] });
        Assert.Empty(factory.DescribeMediaDevices(removed)); Assert.DoesNotContain(factory.GetFirmwareSlots(removed), firmware => firmware.FieldId is FloppyExtensionConstants.IntelRom or FloppyExtensionConstants.WdRom && firmware.IsRequired);
        Assert.Throws<ArgumentException>(() => factory.ApplyStorage(empty, settings with { DeviceSettings = [new(slot, InterfaceId: "IDE")] }));
    }
}
