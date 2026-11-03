using Controller = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services.Wd1770Drive;
using Disk = RetroMediaLab.Tests.Emulation.Wd1770.MemoryTrackDisk;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using ModelAMachine = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Protocole WD1770 exercé avec une image empruntée entièrement en mémoire.</summary>
public sealed class AcornEmuWd1770Tests
{
    [Theory]
    [InlineData(1_000_000)]
    [InlineData(3_000_001)]
    [InlineData(8_000_000)]
    [InlineData(16_000_000)]
    public void ReaderKeepsItsSiliconClockWhenTheOwnerClockIsDifferent(int ownerClock)
    {
        using var disk = new Disk(0x55);
        var controller = new Controller(_ => disk, ticksPerSecond: ownerClock);
        controller.Write(2, 0);
        controller.Write(0, 0x88);
        var bytes = new List<byte>();
        var ticks = 0;
        for (; ticks < ownerClock / 2 && controller.Active; ticks++)
        {
            controller.Tick();
            if (controller.Drq) bytes.Add(controller.Read(3));
        }
        Assert.False(controller.Active);
        Assert.Equal(0, controller.Read(0) & 0x7c);
        Assert.Equal(disk.Bytes(0, 0, 0), bytes);
        Assert.Equal((ulong)((long)ticks * 8_000_000 / ownerClock) * 2, controller.Chip.HalfClocks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EventAdvanceMatchesIndividualTicksAcrossTransfersLostDataAndIndex(bool wd1772)
    {
        using var diskA = new Disk(25);
        using var diskB = new Disk(25);
        var model = wd1772 ? RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Model.Wd1772
            : RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Model.Wd1770;
        var cycle = new Controller(_ => diskA, model: model);
        var events = new Controller(_ => diskB, model: model);
        foreach (byte command in new byte[] { 0x5a, 0x88, 0xa8, 0xd4, 0x08 })
        {
            cycle.Write(2, 0);
            events.Write(2, 0);
            cycle.Write(0, command);
            events.Write(0, command);
            foreach (var ticks in new[] { 0, 1, 63, 64, 95, 1000, 50000, 800003 })
            {
                for (var tick = 0; tick < ticks; tick++) cycle.Tick();
                events.Advance(ticks);
                Assert.Equal(cycle.Irq, events.Irq);
                Assert.Equal(cycle.Drq, events.Drq);
                Assert.Equal(cycle.Active, events.Active);
                Assert.Equal(cycle.Motor, events.Motor);
                Assert.Equal(cycle.Read(0), events.Read(0));
                Assert.Equal(cycle.Read(1), events.Read(1));
                Assert.Equal(cycle.Read(2), events.Read(2));
                Assert.Equal(cycle.Read(3), events.Read(3));
                Assert.Equal(cycle.SeekSteps(0), events.SeekSteps(0));
                Assert.Equal(cycle.ReadBytes(0), events.ReadBytes(0));
                Assert.Equal(cycle.WrittenBytes(0), events.WrittenBytes(0));
            }
        }
    }

    [Theory]
    [InlineData(false, 0, 6)]
    [InlineData(false, 1, 12)]
    [InlineData(false, 2, 20)]
    [InlineData(false, 3, 30)]
    [InlineData(true, 0, 6)]
    [InlineData(true, 1, 12)]
    [InlineData(true, 2, 2)]
    [InlineData(true, 3, 3)]
    public void SteppingCadenceBelongsToTheControllerSilicon(bool wd1772, int rate, int milliseconds)
    {
        var model = wd1772 ? RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Model.Wd1772
            : RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Model.Wd1770;
        var controller = new Controller(_ => null, model: model);
        controller.Write(3, 2);
        controller.Write(0, (byte)(0x18 | rate));
        for (var tick = 0; tick < 100 && controller.SeekSteps(0) == 0; tick++) controller.Tick();
        Assert.Equal(1, controller.SeekSteps(0));
        var interval = 0;
        while (controller.SeekSteps(0) == 1 && interval <= milliseconds * 2000)
        {
            controller.Tick();
            interval++;
        }
        Assert.Equal(milliseconds * 2000, interval);
        Assert.Equal(2, controller.SeekSteps(0));
        Assert.Equal(2, controller.Read(1));
        controller.Advance(milliseconds * 2000);
        Assert.False(controller.Active);
    }

    [Fact]
    public void FourDriveBoardKeepsIndependentHeadPositionsAndMedia()
    {
        using var first = new Disk(10);
        using var second = new Disk(20);
        using var third = new Disk(30);
        using var fourth = new Disk(40);
        Disk[] disks = [first, second, third, fourth];
        var controller = new Controller(index => disks[index],
            model: RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums.Wd1770Model.Wd1772, driveCount: 4)
        { MechanicalSpeedPercent = 0 };
        for (var drive = 0; drive < disks.Length; drive++)
        {
            controller.Drive = drive;
            controller.Write(1, 0);
            Seek(controller, (byte)(drive % 3));
            controller.Write(2, 0);
            controller.Write(0, 0x88);
            Assert.Equal(disks[drive].Bytes(drive % 3, 0, 0), Transfer(controller).Data);
        }
        for (var drive = 0; drive < disks.Length; drive++) Assert.Equal(drive % 3, controller.SeekSteps(drive));
        controller.Reset();
        Assert.All(disks, disk => Assert.False(disk.Disposed));
    }

    [Fact]
    public void DfsCanProbeTrackRegisterBeforeReleasingControllerReset()
    {
        var machine = new ModelAMachine(new byte[16384], new byte[16384], diskInterface: true, wd1770: true);
        machine.Write(0xfe85, 0x5a);
        Assert.Equal(0x5a, machine.Read(0xfe85));
        machine.Write(0xfe86, 3);
        Assert.Equal(3, machine.Read(0xfe86));
        machine.Write(0xfe84, 0x88);
        for (var tick = 0; tick < 100; tick++) machine.Tick();
        Assert.False(machine.FloppyMotor(0));
        Assert.False(machine.FloppyReading(0));
        Assert.False(machine.Nmi);
    }

    [Fact]
    public void AcornControlLatchAndRegistersReachTheSelectedSideAndNmi()
    {
        using var disk = new Disk(80);
        var machine = new ModelAMachine(new byte[16384], new byte[16384], diskInterface: true, wd1770: true);
        try
        {
            machine.AttachFloppy(1, disk); machine.ConfigureFloppySpeed(1, 0);
            machine.Write(0xfe80, 0x26); // Reset désactivé, lecteur 1, face 1, double densité.
            machine.Write(0xfe86, 0); machine.Write(0xfe84, 0x88);
            for (var tick = 0; tick < 100_000 && !machine.Nmi; tick++) machine.Tick();
            Assert.True(machine.Nmi); Assert.True(machine.FloppyActive(1)); Assert.False(machine.FloppyActive(0));
            Assert.Equal(disk.Bytes(0, 1, 0)[0], machine.Read(0xfe87)); Assert.False(machine.Nmi);
            machine.Write(0xfe80, 0); Assert.False(machine.Nmi); Assert.False(machine.FloppyActive(1)); Assert.False(disk.Disposed);
        }
        finally { machine.AttachFloppy(1, null); }
    }

    [Fact]
    public void ControllerChoiceBelongsToExtensionsAndFiltersDoubleDensityDriveModels()
    {
        var factory = new AcornEmuMachineFactory();
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var storage = factory.DescribeStorage(configuration);
        configuration = factory.ApplyStorage(configuration, storage with { ConfiguredSlots = [EmulationMediaSlot.Floppy0, EmulationMediaSlot.Floppy1] });
        storage = factory.DescribeStorage(configuration);
        var settings = storage.DeviceSettings!.Select(device => device with
        {
            Floppy = device.Floppy! with { Model = StorageConstants.Double80Dd }
        }).ToArray();
        Assert.Throws<ArgumentException>(() => factory.ApplyStorage(configuration, storage with { DeviceSettings = settings }));
        var intelExtensions = factory.DescribeStorage(configuration).AvailableDevices.SelectMany(device => device.AcceptedExtensions).ToArray();
        Assert.DoesNotContain(".adl", intelExtensions);
        configuration = configuration with { Options = new Dictionary<string, string>(configuration.Options)
        {
            [StorageConstants.ControllerOption] = StorageConstants.AcornWd1770
        } };
        factory.ValidateOptions(configuration);
        configuration = factory.ApplyStorage(configuration, factory.DescribeStorage(configuration) with { DeviceSettings = settings });
        Assert.All(factory.DescribeStorage(configuration).DeviceSettings!, device => Assert.Equal(StorageConstants.Double80Dd, device.Floppy!.Model));
        Assert.Contains(".adl", factory.DescribeStorage(configuration).AvailableDevices.SelectMany(device => device.AcceptedExtensions));
        Assert.True(factory.RequiresOptionRestart(configuration, StorageConstants.ControllerOption));
        var field = Assert.Single(factory.DescribeSettings(configuration).SelectMany(block => block.Fields), field => field.Id == StorageConstants.ControllerOption);
        Assert.Equal(EmulationMachineTab.Extensions, field.Tab);
        Assert.True(field.UseRadioButtons);
        Assert.Equal(StorageConstants.AcornWd1770, field.Value);
        Assert.All(factory.DescribeStorage(configuration).AvailableDevices.Where(device => device.MediaType == EmulationMediaType.Floppy),
            device => Assert.DoesNotContain(device.ConfigurationFields!, field => field.Id == StorageConstants.ControllerOption));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void ReadsAndWritesTheSelectedPhysicalSide(int drive, int side)
    {
        using var first = new Disk(1); using var second = new Disk(70);
        var controller = new Controller(index => index == 0 ? first : second) { Drive = drive, Side = side, MechanicalSpeedPercent = 0 };
        var disk = drive == 0 ? first : second;
        Seek(controller, 2); controller.Write(2, 3); controller.Write(0, 0x88);
        var read = Transfer(controller); Assert.Equal(0, read.Status & 0x7c); Assert.Equal(disk.Bytes(2, side, 3), read.Data);
        var replacement = Enumerable.Range(0, 256).Select(index => (byte)(index ^ 0xa5)).ToArray();
        controller.Write(0, 0xa8); Assert.Equal(0, Transfer(controller, replacement).Status & 0x7c);
        Assert.Equal(replacement, disk.Bytes(2, side, 3)); Assert.NotEqual(replacement, disk.Bytes(2, 1 - side, 3));
        Assert.True(disk.HasChanges);
        controller.Write(0, 0x88);
        var reread = Transfer(controller);
        Assert.Equal(0, reread.Status & 0x7c);
        Assert.Equal(replacement, reread.Data);
    }

    [Fact]
    public void MultiSectorReadStopsAtTheMissingNextIdAndForcedInterruptCanCancelIt()
    {
        using var disk = new Disk(); var controller = new Controller(_ => disk) { MechanicalSpeedPercent = 0 };
        controller.Write(2, 14); controller.Write(0, 0x98);
        var read = Transfer(controller); Assert.Equal(16, read.Status & 16); Assert.Equal(disk.Bytes(0, 0, 14).Concat(disk.Bytes(0, 0, 15)), read.Data);
        controller.Write(2, 0); controller.Write(0, 0x98);
        for (var tick = 0; tick < 400_000 && !controller.Drq && controller.Active; tick++) controller.Tick();
        Assert.True(controller.Drq);
        Assert.True(controller.Nmi); controller.Write(0, 0xd0);
        Assert.False(controller.Nmi); Assert.False(controller.Active);
        controller.Write(0, 0xd8); Assert.True(controller.Irq); controller.Read(0); Assert.True(controller.Irq);
        controller.Write(0, 0xd0); Assert.False(controller.Irq);
    }

    [Fact]
    public void ReadAddressAndTrackContainThePhysicalIdsAndDataAndTrackCanBeRewritten()
    {
        using var disk = new Disk(); var controller = new Controller(_ => disk) { MechanicalSpeedPercent = 0, Side = 1 };
        Seek(controller, 1); controller.Write(0, 0xc8);
        var address = Transfer(controller).Data;
        Assert.Equal(6, address.Length);
        Assert.Equal(new byte[] { 1, 1 }, address.Take(2));
        Assert.InRange(address[2], (byte)0, (byte)15);
        Assert.Equal(1, address[3]);
        controller.Write(0, 0xe8);
        var track = Transfer(controller);
        Assert.InRange(track.Data.Length, 6240, 6250); // La synchronisation sur AM peut modifier l'alignement des gaps (WD 1-13).
        Assert.Equal(0, track.Status & 0x7c);
        var mark = Find(track.Data, [0xa1, 0xa1, 0xa1, 0xfb]);
        Assert.True(mark >= 0);
        Assert.Equal(disk.Bytes(1, 1, 0), track.Data.AsSpan(mark + 4, 256).ToArray());
        var format = new List<byte>();
        format.AddRange(Enumerable.Repeat((byte)0x4e, 80));
        for (byte sector = 0; sector < 16; sector++)
        {
            format.AddRange(new byte[12]);
            format.AddRange(new byte[] { 0xf5, 0xf5, 0xf5, 0xfe, 1, 1, sector, 1, 0xf7 });
            format.AddRange(Enumerable.Repeat((byte)0x4e, 22));
            format.AddRange(new byte[12]);
            format.AddRange(new byte[] { 0xf5, 0xf5, 0xf5, 0xfb });
            format.AddRange(Enumerable.Repeat((byte)0x99, 256));
            format.Add(0xf7);
            format.AddRange(Enumerable.Repeat((byte)0x4e, 24));
        }
        var offset = 0;
        controller.Write(0, 0xf8);
        for (var tick = 0; tick < 800_000 && controller.Active; tick++)
        {
            if (controller.Drq) controller.Write(3, offset < format.Count ? format[offset++] : (byte)0x4e);
            controller.Tick();
        }
        Assert.False(controller.Active);
        Assert.Equal(format.Count, offset);
        Assert.Equal(0, controller.Read(0) & 0x7c);
        for (byte sector = 0; sector < 16; sector++)
        {
            controller.Write(2, sector);
            controller.Write(0, 0x88);
            var read = Transfer(controller);
            Assert.Equal(0, read.Status & 0x7c);
            Assert.Equal(Enumerable.Repeat((byte)0x99, 256), read.Data);
            Assert.NotEqual((byte)0x99, disk.Bytes(1, 0, sector)[0]);
        }
    }

    [Fact]
    public void ProtectionDensityCrcMissingMediaAndUnservicedBytesAreReported()
    {
        using var disk = new Disk { IsReadOnly = true };
        var controller = new Controller(_ => disk) { MechanicalSpeedPercent = 0 };
        controller.Write(0, 0xa8); Assert.Equal(64, Transfer(controller).Status & 64); Assert.False(disk.HasChanges);
        controller.DoubleDensity = false; controller.Write(0, 0x88); Assert.Equal(16, Transfer(controller).Status & 16);
        controller.DoubleDensity = true; disk.Bad = true; controller.MediaChanged(0); controller.Write(2, 0); controller.Write(0, 0x88);
        var crc = Transfer(controller); Assert.Equal(8, crc.Status & 8); Assert.Equal(256, crc.Data.Length);
        disk.Bad = false; controller.MediaChanged(0); controller.Write(0, 0x88);
        for (var tick = 0; tick < 400_000 && controller.Active; tick++) controller.Tick();
        Assert.False(controller.Active); Assert.Equal(4, controller.Read(0) & 4);
        var absent = new Controller(_ => null);
        absent.Write(0, 0x88);
        absent.Advance(400_000);
        Assert.True(absent.Active);
        Assert.False(absent.Drq);
        Assert.False(absent.Irq);
        absent.Write(0, 0xd0);
        Assert.False(absent.Active);
    }

    [Fact]
    public void StepRegisterUpdatesAndResetRespectTheMechanicalPositionAndBorrowedImage()
    {
        using var disk = new Disk(); var controller = new Controller(_ => disk) { MechanicalSpeedPercent = 0 };
        controller.Write(0, 0x48); Transfer(controller); Assert.Equal(0, controller.Read(1));
        controller.Write(0, 0x58); Transfer(controller); Assert.Equal(1, controller.Read(1));
        controller.Write(0, 0x08); Transfer(controller); Assert.Equal(0, controller.Read(1)); Assert.Equal(4, controller.Read(0) & 4);
        controller.Reset(); Assert.False(controller.Nmi); Assert.False(controller.Motor); Assert.False(disk.Disposed);
    }

    [Fact]
    public void RealSpeedWaitsForMotorSpinupBeforeRequestingBytes()
    {
        using var disk = new Disk(); var controller = new Controller(_ => disk);
        controller.Write(2, 0); controller.Write(0, 0x80);
        for (var tick = 0; tick < 400_000; tick++) controller.Tick();
        Assert.True(controller.Active); Assert.False(controller.Drq);
        Assert.Equal(256, Transfer(controller).Data.Length);
    }

    private static void Seek(Controller controller, byte track) { controller.Write(3, track); controller.Write(0, 0x18); Transfer(controller); }
    private static (byte Status, byte[] Data) Transfer(Controller controller, byte[]? write = null)
    {
        var bytes = new List<byte>(); var offset = 0;
        for (var tick = 0; tick < 4_000_000; tick++)
        {
            controller.Tick();
            if (controller.Drq)
            {
                Assert.True(controller.Nmi);
                if (write is null) bytes.Add(controller.Read(3)); else { Assert.True(offset < write.Length); controller.Write(3, write[offset++]); }
                Assert.False(controller.Drq);
            }
            if (!controller.Active)
            {
                Assert.True(controller.Irq); var status = controller.Read(0); Assert.False(controller.Irq);
                if (write is not null) Assert.Equal(write.Length, offset);
                return (status, bytes.ToArray());
            }
        }
        Assert.Fail("WD1770 command timed out."); return default;
    }
    private static int Find(byte[] bytes, byte[] pattern)
    {
        for (var index = 0; index <= bytes.Length - pattern.Length; index++) if (bytes.AsSpan(index, pattern.Length).SequenceEqual(pattern)) return index;
        return -1;
    }

}
