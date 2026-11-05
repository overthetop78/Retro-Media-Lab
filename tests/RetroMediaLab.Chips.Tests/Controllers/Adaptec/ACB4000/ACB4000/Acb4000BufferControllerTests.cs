using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000BufferControllerTests
{
    [Fact]
    public void PointerBytesAreIndependentAndCpuAccessDoesNotIncrementThem()
    {
        var ram = new byte[1024];
        var controller = Create(ram);
        WritePointer(controller, 0x5a, 0x0123);
        WritePointer(controller, 0x5c, 0x0234);
        WritePointer(controller, 0x5e, 0x0345);
        controller.TryWrite(0x70, 0xa6);
        Assert.Equal(0xa6, ram[0x123]);
        Assert.True(controller.TryRead(0x70, out var value));
        Assert.Equal(0xa6, value);
        Assert.Equal(0x0123, controller.ReadPointer);
        Assert.Equal(0x0234, controller.WritePointer);
        Assert.Equal(0x0345, controller.StopPointer);
        Assert.True(controller.TryRead(0x5a, out value));
        Assert.Equal(0x23, value);
        controller.TryRead(0x5b, out value);
        Assert.Equal(1, value);
    }

    [Fact]
    public void RopSelectsTheWritePointerForBothCpuDirections()
    {
        var ram = new byte[1024];
        var controller = Create(ram);
        WritePointer(controller, 0x5a, 0x0111);
        WritePointer(controller, 0x5c, 0x0222);
        controller.TryWrite(0x53, 0x10);
        controller.TryWrite(0x70, 0x5a);
        Assert.True(controller.TryRead(0x70, out var value));
        Assert.Equal(0x5a, value);
        Assert.Equal(0, ram[0x111]);
        Assert.Equal(0x5a, ram[0x222]);
        Assert.Equal(0x0111, controller.ReadPointer);
        Assert.Equal(0x0222, controller.WritePointer);
    }

    [Theory]
    [InlineData(0, 0x00ff)]
    [InlineData(1, 0x01ff)]
    [InlineData(3, 0x03ff)]
    [InlineData(0xff, 0xffff)]
    public void PeripheralTransfersWrapAtProgrammedSizeAndIgnoreStop(byte size, ushort last)
    {
        var ram = new byte[1024];
        ram[last & 0x3ff] = 0x73;
        var controller = Create(ram);
        controller.TryWrite(0x54, size);
        WritePointer(controller, 0x5a, last);
        WritePointer(controller, 0x5c, last);
        WritePointer(controller, 0x5e, last);
        Assert.True(controller.TryTransferPeripheral(0, out var value));
        Assert.Equal(0x73, value);
        Assert.Equal(0, controller.ReadPointer);
        Assert.Equal(last, controller.WritePointer);
        controller.TryWrite(0x53, 0x10);
        Assert.True(controller.TryTransferPeripheral(0xc9, out _));
        Assert.Equal(0xc9, ram[last & 0x3ff]);
        Assert.Equal(0, controller.WritePointer);
        Assert.True(controller.TryTransferPeripheral(0xb6, out _));
        Assert.Equal(0xb6, ram[0]);
        controller.TryRead(0x53, out value);
        Assert.Equal(0, value & 0x20);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HostDmaUsesTheOppositePointerAndIncludesTheStopAddress(bool readingPeripheral)
    {
        var ram = new byte[] { 0x31, 0x42, 0x53, 0x64 };
        var controller = Create(ram);
        controller.TryWrite(0x53, readingPeripheral ? (byte)0xd8 : (byte)0xc4);
        WritePointer(controller, 0x5e, 1);
        for (var index = 0; index < 2; index++)
        {
            Assert.True(controller.TryTransferHost((byte)(index + 0xa0), out var value));
            if (readingPeripheral) Assert.Equal(index == 0 ? 0x31 : 0x42, value);
            else Assert.Equal(index + 0xa0, ram[index]);
        }
        Assert.Equal(readingPeripheral ? 2 : 0, controller.ReadPointer);
        Assert.Equal(readingPeripheral ? 0 : 2, controller.WritePointer);
        Assert.False(controller.HostTransferEnabled);
        Assert.False(controller.TryTransferHost(0, out _));
        controller.TryRead(0x53, out var status);
        Assert.NotEqual(0, status & 0x20);
        controller.TryWrite(0x5e, 2);
        Assert.True(controller.HostTransferEnabled);
        Assert.True(controller.TryTransferHost(0xa2, out _));
        Assert.False(controller.HostTransferEnabled);
    }

    [Fact]
    public void DmaDoneCannotBeWrittenAndWrongLatchDoesNotStartHostTransfer()
    {
        var controller = Create(new byte[1024]);
        controller.TryWrite(0x53, 0xf4); // ROP with WRITE latch; no host read latch.
        Assert.False(controller.HostTransferEnabled);
        Assert.False(controller.TryTransferHost(0x66, out _));
        controller.TryRead(0x53, out var value);
        Assert.Equal(0xd4, value);
        Assert.Equal(0, controller.ReadPointer);
        Assert.Equal(0, controller.WritePointer);
        controller.TryWrite(0x53, 0xc8); // WOP with READ latch; no host write latch.
        Assert.False(controller.HostTransferEnabled);
    }

    [Fact]
    public void EveryResetControlWriteClearsPointersWithoutErasingRamOrOrdinaryControl()
    {
        var ram = new byte[1024];
        var controller = Create(ram);
        controller.TryWrite(0x53, 0xd8);
        WritePointer(controller, 0x5a, 0x0100);
        WritePointer(controller, 0x5c, 0x0200);
        WritePointer(controller, 0x5e, 0x0300);
        ram[0x200] = 0x83;
        controller.TryWrite(0x59, 0);
        Assert.Equal(0, controller.ReadPointer);
        Assert.Equal(0, controller.WritePointer);
        Assert.Equal(0, controller.StopPointer);
        controller.TryRead(0x53, out var value);
        Assert.Equal(0xd8, value);
        Assert.Equal(0x83, ram[0x200]);
        controller.TryWrite(0x59, 1);
        Assert.True(controller.ResetAsserted);
        controller.TryWrite(0x5c, 0x55);
        controller.TryWrite(0x70, 0x44);
        Assert.Equal(0, controller.WritePointer);
        Assert.Equal(0, ram[0]);
        Assert.False(controller.TryRead(0x70, out _));
        Assert.False(controller.TryTransferPeripheral(0, out _));
        controller.TryWrite(0x59, 0);
        controller.TryRead(0x53, out value);
        Assert.Equal(0, value);
    }

    [Fact]
    public void UndecodedRegistersDoNotAliasThePointerBank()
    {
        var controller = Create(new byte[1024]);
        foreach (var register in new byte[] { 0x50, 0x51, 0x55, 0x58, 0x60, 0x71 })
        {
            Assert.False(controller.TryRead(register, out _));
            Assert.False(controller.TryWrite(register, 0xff));
        }
        Assert.False(controller.TryRead(0x54, out _));
        Assert.False(controller.TryRead(0x59, out _));
    }

    [Fact]
    public void SteadyPeripheralCyclesDoNotAllocate()
    {
        var controller = Create(new byte[1024]);
        controller.TryWrite(0x53, 0x10);
        for (var index = 0; index < 1000; index++) controller.TryTransferPeripheral((byte)index, out _);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10000; index++) controller.TryTransferPeripheral((byte)index, out _);
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LastRamCycleAdvancesPointerButDoneWaitsForCompletedAcknowledgement(bool read)
    {
        var ram = new byte[1024];
        ram[0] = 0x7a;
        var controller = Create(ram);
        controller.TryWrite(0x53, read ? (byte)0xd8 : (byte)0xc4);
        if (read)
        {
            Assert.True(controller.TryBeginHostRead(out var value));
            Assert.Equal(0x7a, value);
            Assert.Equal(1, controller.ReadPointer);
            Assert.False(controller.TryBeginHostRead(out _));
        }
        else
        {
            Assert.True(controller.TryBeginHostWrite(0xa9));
            Assert.Equal(0xa9, ram[0]);
            Assert.Equal(1, controller.WritePointer);
            Assert.False(controller.TryBeginHostWrite(0x55));
        }
        Assert.True(controller.HostTransferPending);
        controller.TryRead(0x53, out var status);
        Assert.Equal(0, status & 0x20);
        Assert.True(controller.CompleteHostAcknowledgement());
        Assert.False(controller.HostTransferPending);
        controller.TryRead(0x53, out status);
        Assert.NotEqual(0, status & 0x20);
        Assert.False(controller.CompleteHostAcknowledgement());
    }

    [Fact]
    public void ResetCancelsPendingAcknowledgementWithoutInventingDoneOrUndoingRamWrite()
    {
        var ram = new byte[1024];
        var controller = Create(ram);
        controller.TryWrite(0x53, 0xc4);
        Assert.True(controller.TryBeginHostWrite(0x83));
        controller.TryWrite(0x59, 0);
        Assert.False(controller.CompleteHostAcknowledgement());
        Assert.False(controller.HostTransferPending);
        controller.TryRead(0x53, out var status);
        Assert.Equal(0, status & 0x20);
        Assert.Equal(0, controller.WritePointer);
        Assert.Equal(0x83, ram[0]);
    }

    [Fact]
    public void CancellingAPendingCycleKeepsItsRamAndPointerWithoutPublishingDone()
    {
        var ram = new byte[1024];
        var controller = Create(ram);
        controller.TryWrite(0x53, 0xc4);
        Assert.True(controller.TryBeginHostWrite(0x59));
        controller.CancelHostTransfer();
        Assert.Equal(0x59, ram[0]);
        Assert.Equal(1, controller.WritePointer);
        Assert.False(controller.CompleteHostAcknowledgement());
        Assert.False(controller.HostTransferPending);
        Assert.False(controller.HostTransferEnabled);
        controller.TryRead(0x53, out var status);
        Assert.Equal(0xc0, status);
    }

    private static Acb4000BufferController Create(byte[] ram)
    {
        var controller = new Acb4000BufferController(ram);
        controller.TryWrite(0x59, 0);
        controller.TryWrite(0x54, 3);
        return controller;
    }

    private static void WritePointer(Acb4000BufferController controller, byte lowRegister, ushort value)
    {
        Assert.True(controller.TryWrite(lowRegister, (byte)value));
        Assert.True(controller.TryWrite((byte)(lowRegister + 1), (byte)(value >> 8)));
    }
}
