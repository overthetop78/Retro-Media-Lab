using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Services;
using RetroMediaLab.Chips.Memory.Acorn.Archimedes.MEMC.Services;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Enums;
using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;
using System.Buffers.Binary;
using RetroMediaLab.Chips.IO.Acorn.Archimedes.Keyboard.Services;
using static RetroMediaLab.Chips.Tests.Shared.ArchimedesTestCircuits;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.Archimedes.IOC;
public sealed class IocTests
{
    [Fact]
    public void IocInterruptMasksAndClearRespectLatchedAndLevelInputs()
    {
        var ioc = new ArchimedesIoc();
        ioc.Reset(cold: true);
        Assert.Equal(0x90, ioc.Read(0x10));
        Assert.False(ioc.Irq);
        ioc.Write(0x18, 0x10);
        Assert.True(ioc.Irq);
        ioc.Write(0x14, 0x10);
        Assert.False(ioc.Irq);
        ioc.SetIrqA(1, true);
        ioc.Write(0x14, 1);
        Assert.Equal(0x81, ioc.Read(0x10));
        ioc.Write(0x38, 0x80);
        Assert.True(ioc.Fiq);
        ioc.Write(0x38, 0);
        ioc.SetFlyback(true);
        ioc.Write(0x14, 8);
        ioc.SetFlyback(true);
        Assert.Equal(0, ioc.Read(0x10) & 8);
        ioc.SetFlyback(false);
        ioc.SetFlyback(true);
        Assert.Equal(8, ioc.Read(0x10) & 8);
    }

    [Fact]
    public void IocTimerOutputIsLatchedAndPeriodsDoNotDependOnCpuSpeed()
    {
        var ioc = new ArchimedesIoc();
        ioc.Reset(cold: false);
        ioc.Write(0x40, 3);
        ioc.Write(0x44, 0);
        ioc.Write(0x48, 0);
        ioc.Advance(2);
        ioc.Write(0x4c, 0);
        Assert.Equal(1, ioc.Read(0x40));
        ioc.Advance(2);
        Assert.Equal(1, ioc.Read(0x40));
        Assert.Equal(0x20, ioc.Read(0x10) & 0x20);
        ioc.Write(0x14, 0x20);
        ioc.Advance(3);
        Assert.Equal(0, ioc.Read(0x10) & 0x20);
        ioc.Advance(1);
        Assert.Equal(0x20, ioc.Read(0x10) & 0x20);
    }

    [Fact]
    public void IocKartUsesEightDataAndTwoStopBitsAndAcknowledgesReceiveByRead()
    {
        var ioc = new ArchimedesIoc();
        ioc.Reset(cold: false);
        ioc.Write(0x70, 1);
        ioc.Write(0x78, 0);
        byte? transmitted = null;
        ioc.KeyboardTransmitted = value => transmitted = value;
        try
        {
            ioc.Write(4, 0x55);
            Assert.True(ioc.ReceiveKeyboardByte(0xa5));
            ioc.Advance(543);
            Assert.Equal(0, ioc.Read(0x20) & 0xc0);
            ioc.Advance(1);
            Assert.Equal(0xa5, ioc.Read(4));
            Assert.Equal(0, ioc.Read(0x20) & 0x80);
            Assert.True(ioc.KeyboardReceiveBusy);
            ioc.Advance(160);
            Assert.False(ioc.KeyboardReceiveBusy);
            Assert.Equal((byte)0x55, transmitted);
            Assert.Equal(0x40, ioc.Read(0x20) & 0x40);
        }
        finally
        {
            ioc.KeyboardTransmitted = null;
        }
    }
}
