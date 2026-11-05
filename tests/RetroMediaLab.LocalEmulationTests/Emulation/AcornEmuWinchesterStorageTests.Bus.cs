using RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using H = RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Constants.ScsiHostAdapterConstants;
using Formats = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.HardDiskFormats;
using Media = RetroMediaLab.Emulation.Acorn.Common.Constants.MediaConstants;
using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuWinchesterStorageTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(800)]
    [InlineData(0)]
    public void BbcWinchesterUsesThePhysicalControllerAndItsCountersAtEveryOfferedSpeed(int speed)
    {
        using var image = new Image(256, 33);
        _ = SignalStorage(image);
        var bus = new Winchester(true);
        try
        {
            bus.Configure(0, 2, speed, 2);
            bus.Attach(0, image);
            var result = HostCommand(register => bus.Read((ushort)register),
                (register, value) => bus.Write((ushort)register, value), bus.Tick,
                () => (bus.Read(H.StatusRegister) & (byte)ScsiBusSignals.Busy) != 0,
                [8, 0, 0, 1, 1, 0]);
            Assert.Equal(0, result.Status);
            Assert.Equal(image.Data.AsSpan(256, 256).ToArray(), result.Data);
            Assert.Equal(32, bus.SeekCount(0));
            Assert.Equal(1, bus.ReadBlocks(0));
            Assert.Equal(0, bus.WrittenBlocks(0));
            Assert.False(bus.Active(0));
            Assert.True(bus.Motor(0));
            Assert.Equal(2, Storage.HardDiskCount);
            Assert.Throws<ArgumentOutOfRangeException>(() => bus.Attach(2, image));
            bus.Reset();
            Assert.Equal(0, bus.SeekCount(0));
            Assert.Equal(0, bus.ReadBlocks(0));
            bus.Attach(0, null);
            Assert.False(bus.Motor(0));
            Assert.False(image.Drive.MotorRunning);
            Assert.False(image.Disposed);
        }
        finally { bus.Reset(); }
    }

    [Theory]
    [InlineData(0, 2, 2, true)]
    [InlineData(256, 2, 2, true)]
    [InlineData(512, 2048, 16, true)]
    [InlineData(1024, 1, 1, true)]
    [InlineData(128, 2, 2, false)]
    [InlineData(256, 2049, 2, false)]
    [InlineData(256, 2, 17, false)]
    [InlineData(256, 0, 2, false)]
    public void PhysicalEmuAcceptanceUsesTheControllerGeometryIncludingAnUnformattedImage(int sectorSize, int cylinders, int heads, bool accepted)
    {
        using var image = new Image(sectorSize, 33)
        {
            FormatId = Media.WinchesterPhysicalFormatId,
            Cylinders = cylinders,
            Heads = heads
        };
        Assert.Equal(accepted, Formats.Supports(image));
    }

    [Fact]
    public void WinchesterKeepsDatSupportAndRejectsAnotherContainerIdentity()
    {
        using var dat = new Image(256, 33) { FormatId = Media.WinchesterFormatId };
        using var other = new Image(256, 33);
        Assert.True(Formats.Supports(dat));
        Assert.False(Formats.Supports(other));
    }

    [Theory]
    [InlineData(2048, 16, 33, true)]
    [InlineData(2048, 16, 32, true)]
    [InlineData(2049, 16, 33, false)]
    [InlineData(2048, 17, 33, false)]
    [InlineData(0, 2, 33, false)]
    [InlineData(2, 0, 33, false)]
    [InlineData(2, 2, 34, false)]
    public void DatAcceptanceRejectsGeometryOutsideThePhysicalController(int cylinders, int heads, int sectors, bool accepted)
    {
        using var image = new Image(256, sectors)
        {
            FormatId = Media.WinchesterFormatId,
            Cylinders = cylinders,
            Heads = heads
        };
        Assert.Equal(accepted, Formats.Supports(image));
    }

    [Fact]
    public void OfferedCustomCapacityDoesNotExceedTheAcb4000Geometry()
    {
        const long maximum = 2048L * 16 * 33 * 256;
        Assert.All(Formats.All, format => Assert.InRange(format.MaximumBytes, 1, maximum));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void HostRegistersReadTheSelectedPhysicalDiskWithoutCallingTheTargetBus(int unit)
    {
        using var first = new Image(256, 33);
        using var second = new Image(256, 33);
        first.Data.AsSpan().Fill(0x11);
        second.Data.AsSpan().Fill(0x22);
        WinchesterStorage[] stores = [SignalStorage(first), SignalStorage(second)];
        var controller = new WinchesterController(index => stores[index]);
        try
        {
            var mode = HostCommand(controller, [0x1a, (byte)(unit << 5), 0, 0, 22, 0]);
            Assert.Equal(0, mode.Status);
            Assert.Equal(22, mode.Data.Length);
            var read = HostCommand(controller, [8, (byte)(unit << 5), 0, 1, 1, 0]);
            Assert.Equal(0, read.Status);
            Assert.Equal(256, read.Data.Length);
            Assert.All(read.Data, value => Assert.Equal(unit == 0 ? (byte)0x11 : (byte)0x22, value));
            Assert.False(controller.Busy);
            Assert.False(first.Disposed);
            Assert.False(second.Disposed);
        }
        finally { controller.Reset(); }
    }

    [Fact]
    public void HostReadsSenseForAMissingPortWithoutInventingExtraPhysicalDrives()
    {
        var controller = new WinchesterController(_ => null);
        try
        {
            Assert.Equal(2, HostCommand(controller, [0, 0x40, 0, 0, 0, 0]).Status);
            var result = HostCommand(controller, [3, 0x40, 0, 0, 4, 0]);
            Assert.Equal(0, result.Status);
            Assert.Equal(0x25, result.Data[0]);
            Assert.False(controller.Busy);
        }
        finally { controller.Reset(); }
    }

    [Fact]
    public void HostInterruptLatchAndResetFollowThePhysicalHandshake()
    {
        using var image = new Image(256, 33);
        var storage = SignalStorage(image);
        var controller = new WinchesterController(unit => unit == 0 ? storage : null);
        try
        {
            controller.Write(H.InterruptEnableRegister, H.InterruptEnableMask);
            Assert.Equal(0, HostCommand(controller, [0, 0, 0, 0, 0, 0]).Status);
            Assert.True(controller.Irq);
            Assert.NotEqual(0, controller.Read(H.StatusRegister) & H.InterruptStatusMask);
            controller.Write(H.InterruptEnableRegister, 0);
            Assert.False(controller.Irq);
            controller.Write(H.InterruptEnableRegister, H.InterruptEnableMask);
            controller.Write(H.DataRegister, 1);
            controller.Write(H.SelectRegister, 0);
            WaitHostRequest(controller);
            Assert.True(controller.Busy);
            controller.Reset();
            Assert.False(controller.Busy);
            Assert.False(controller.Irq);
            Assert.Equal(0, controller.Read(H.StatusRegister) & (byte)(ScsiBusSignals.Request | ScsiBusSignals.Busy));
            Assert.Equal(0, HostCommand(controller, [0, 0, 0, 0, 0, 0]).Status);
            Assert.False(image.Disposed);
        }
        finally { controller.Reset(); }
    }

    private static (byte Status, byte[] Data) HostCommand(WinchesterController controller, byte[] command) =>
        HostCommand(controller.Read, controller.Write, controller.Tick, () => controller.Busy, command);

    private static (byte Status, byte[] Data) HostCommand(Func<int, byte> read, Action<int, byte> write,
        Action tick, Func<bool> busy, byte[] command)
    {
        write(H.DataRegister, 1);
        write(H.SelectRegister, 0);
        foreach (var value in command)
        {
            var state = WaitHostRequest(read, tick);
            Assert.NotEqual(0, state & (byte)ScsiBusSignals.ControlData);
            Assert.Equal(0, state & (byte)ScsiBusSignals.InputOutput);
            write(H.DataRegister, value);
            tick();
        }
        var data = new List<byte>();
        byte? status = null;
        while (busy())
        {
            var state = WaitHostRequest(read, tick);
            Assert.NotEqual(0, state & (byte)ScsiBusSignals.InputOutput);
            var value = read(H.DataRegister);
            if ((state & (byte)ScsiBusSignals.ControlData) == 0) data.Add(value);
            else if ((state & (byte)ScsiBusSignals.Message) == 0)
            {
                Assert.Null(status);
                status = value;
            }
            else Assert.Equal(0, value);
            tick();
        }
        Assert.NotNull(status);
        return (status.Value, data.ToArray());
    }

    private static byte WaitHostRequest(WinchesterController controller) => WaitHostRequest(controller.Read, controller.Tick);

    private static byte WaitHostRequest(Func<int, byte> read, Action tick)
    {
        for (var elapsed = 0; elapsed < 2_000_000; elapsed++)
        {
            var state = read(H.StatusRegister);
            if ((state & (byte)ScsiBusSignals.Request) != 0) return state;
            tick();
        }
        Assert.Fail("The host did not observe target REQ.");
        return 0;
    }
}
