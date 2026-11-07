using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Enums;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using C = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Constants.ArchimedesModelConstants;
using S = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Constants.SettingsConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuArchimedesConfigurationTests
{
    [Fact]
    public void A3000KeepsItsMemcSerialUpgradeAndSingleInternalDriveDistinct()
    {
        var configuration = new MachineConfiguration(MachineModel.AcornA3000, core: Emulator.AcornEmu);
        var adapter = new AcornEmuMachineFactory();
        Assert.Equal(MachineFamily.Archimedes, configuration.Family);
        Assert.Equal(ArchimedesMemcRevision.Memc1A, ArchimedesFunctions.MemoryController(configuration));
        Assert.False(ArchimedesFunctions.HasSerialPort(configuration));
        Assert.False(ArchimedesFunctions.HasSt506(configuration));
        Assert.Single(adapter.DescribeStorage(configuration).AvailableDevices);
        Assert.Single(adapter.DescribeMediaDevices(configuration));
        Assert.False(ArchimedesFunctions.SupportsArthur(configuration));
        adapter.ValidateOptions(configuration);
        var options = new Dictionary<string, string>(configuration.Options) { [C.SerialOption] = bool.TrueString };
        configuration = configuration with { Options = options };
        Assert.True(ArchimedesFunctions.HasSerialPort(configuration));
        adapter.ValidateOptions(configuration);
        options[C.St506Option] = bool.TrueString;
        Assert.Throws<ArgumentException>(() => adapter.ValidateOptions(configuration));
    }

    [Theory]
    [InlineData("512")]
    [InlineData("16384")]
    public void A3000DoesNotBorrowTheDesktopRamBoardChoices(string size)
    {
        var configuration = new MachineConfiguration(MachineModel.AcornA3000, core: Emulator.AcornEmu);
        var options = new Dictionary<string, string>(configuration.Options) { [S.MainMemory] = size };
        Assert.Throws<ArgumentException>(() => ArchimedesFunctions.Validate(configuration with { Options = options }));
    }

    [Fact]
    public void A3000IncludesTheDocumentedIfelTwinMemcEightMiBExpansion()
    {
        var configuration = new MachineConfiguration(MachineModel.AcornA3000, core: Emulator.AcornEmu);
        configuration = configuration with
        {
            Options = new Dictionary<string, string>(configuration.Options)
            {
                [S.MainMemory] = C.ExpandedMemoryKib.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        };
        ArchimedesFunctions.Validate(configuration);
        Assert.Equal(C.ExpandedMemoryKib * C.Kibibyte, ArchimedesFunctions.MemoryBytes(configuration));
        Assert.Equal(ArchimedesMemcRevision.Memc1A, ArchimedesFunctions.MemoryController(configuration));
    }

    [Fact]
    public void A3000FirmwareRoleAcceptsRiscOsAndRejectsArthur()
    {
        var configuration = new MachineConfiguration(MachineModel.AcornA3000, core: Emulator.AcornEmu);
        var arthur = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Dictionaries.AcornArchimedesModel.Get(
            "1181ff9c2c2f3d6d414054ec33b2260404bafc81", FirmwareCategory.OperatingSystem, ArchimedesFunctions.SupportsArthur(configuration));
        var riscOs = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Dictionaries.AcornArchimedesModel.Get(
            "3487729e87bebc9cb51665838c27beff22f7b3bd", FirmwareCategory.OperatingSystem, ArchimedesFunctions.SupportsArthur(configuration));
        Assert.Equal(EmulationFirmwareCompatibility.Incompatible, arthur);
        Assert.Equal(EmulationFirmwareCompatibility.Official, riscOs);
    }

    [Fact]
    public void NewCmosCountsThePhysicalDriveSelectsAndAdjustsItsChecksum()
    {
        var time = new DateTime(2024, 1, 1);
        var reference = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesCmosConstants
            .Create(ArchimedesCmosProfile.RiscOs3, time);
        var expanded = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesCmosConstants
            .Create(ArchimedesCmosProfile.RiscOs3, time, 4, 2);
        Assert.Equal(1, reference[0xc7 - 0x10]);
        Assert.Equal(20, expanded[0xc7 - 0x10]);
        Assert.Equal(unchecked((byte)(reference[0x3f - 0x10] + 19)), expanded[0x3f - 0x10]);
    }

    [Fact]
    public void CreatedAdfsDHasAReadableEmptyDirectoryAndTheExpectedFreeSpace()
    {
        var image = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(
            RetroMediaLab.MediaEngine.Constants.DiskImageFormatIds.AcornAdfs800);
        var reader = new RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsFileSystemReader();
        Assert.True(reader.CanRead(image));
        var volume = reader.Read(image);
        Assert.Empty(volume.Entries);
        Assert.Empty(volume.Warnings);
        Assert.Equal(image.Capacity - RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsLayout.DFormatHeaderSize,
            volume.FreeBytes);
    }

    [Fact]
    public void RegisteredConfigurationHasItsOwnFirmwareMemoryAndPointerFacets()
    {
        var configuration = new MachineConfiguration(MachineModel.AcornArchimedes, core: Emulator.AcornEmu);
        var adapter = new AcornEmuMachineFactory();
        Assert.Equal(MachineFamily.Archimedes, configuration.Family);
        Assert.Contains(configuration.MachineId, adapter.Definition.MachineIds);
        Assert.True(adapter.SupportsPointerCapture(configuration));
        Assert.Equal(C.DefaultMemoryKib, adapter.GetMemoryKib(configuration));
        Assert.Single(adapter.GetFirmwareSlots(configuration));
        Assert.DoesNotContain(adapter.DescribeMediaDevices(configuration), device => device.MediaType == EmulationMediaType.Tape);
        Assert.Contains(adapter.DescribeSettings(configuration), block => block.Tab == EmulationMachineTab.Cpu);
        Assert.Contains(adapter.DescribeSettings(configuration), block => block.Tab == EmulationMachineTab.Ram);
        adapter.ValidateOptions(configuration);
    }

    [Fact]
    public void FourFloppySelectionsAndTwoSt506UnitsRemainIndependent()
    {
        var adapter = new AcornEmuMachineFactory();
        var configuration = new MachineConfiguration(MachineModel.AcornArchimedes, core: Emulator.AcornEmu);
        Assert.Equal(4, adapter.DescribeStorage(configuration).AvailableDevices.Count);
        var options = new Dictionary<string, string>(configuration.Options) { [C.St506Option] = bool.TrueString };
        configuration = configuration with { Options = options };
        Assert.Equal(6, adapter.DescribeStorage(configuration).AvailableDevices.Count);
        var third = new EmulationMediaSlot(EmulationMediaCategory.FloppyDrive, 3);
        var secondHardDisk = new EmulationMediaSlot(EmulationMediaCategory.HardDisk, 1);
        configuration = ArchimedesFunctions.SelectSlots(configuration, [EmulationMediaSlot.Floppy0, third, secondHardDisk]);
        var devices = adapter.DescribeMediaDevices(configuration);
        Assert.Equal(new[] { EmulationMediaSlot.Floppy0, third, secondHardDisk }, devices.Select(device => device.Slot));
        var activity = adapter.MediaActivity(configuration, new Dictionary<int, bool> { [6] = true, [9] = true });
        Assert.True(activity[third]);
        Assert.True(activity[secondHardDisk]);
        Assert.False(activity[EmulationMediaSlot.Floppy0]);
        adapter.ValidateOptions(configuration);
    }

    [Fact]
    public void ExpandedMemoryRequiresTheCorrespondingMemoryController()
    {
        var configuration = new MachineConfiguration(MachineModel.AcornArchimedes, core: Emulator.AcornEmu);
        var options = new Dictionary<string, string>(configuration.Options)
        {
            [S.MainMemory] = C.ExpandedMemoryKib.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        Assert.Throws<ArgumentException>(() => ArchimedesFunctions.Validate(configuration with { Options = options }));
        options[C.MemcOption] = nameof(ArchimedesMemcRevision.Memc1A);
        configuration = configuration with { Options = options };
        ArchimedesFunctions.Validate(configuration);
        Assert.Equal(C.ExpandedMemoryKib * C.Kibibyte, ArchimedesFunctions.MemoryBytes(configuration));
    }
}
