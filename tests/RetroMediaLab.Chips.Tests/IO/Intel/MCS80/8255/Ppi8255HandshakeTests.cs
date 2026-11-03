using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

namespace RetroMediaLab.Chips.Tests.IO.Intel.MCS80.Chip8255;

public sealed partial class Ppi8255Tests
{
    [Theory]
    [InlineData(0, 0xbb, 0x10, 0x20, 0x08, 0x09)]
    [InlineData(1, 0x9f, 0x04, 0x02, 0x01, 0x05)]
    public void StrobedInputUsesStbAndRdEdgesAndReportsTheEnableInsteadOfTheStbPin(
        int port, byte control, byte strobe, byte full, byte irq, byte enable)
    {
        byte pins = 0x12;
        var ppi = new Ppi8255 { ReadPortA = () => pins, ReadPortB = () => pins };
        ppi.Write(3, control); ppi.Write(3, enable);
        Assert.Equal(strobe, ppi.Read(2) & strobe);
        ppi.SetPortCInput((byte)~strobe);
        Assert.Equal(full, ppi.Read(2) & full);
        Assert.Equal(0, ppi.Read(2) & irq);
        pins = 0xa5;
        ppi.SetPortCInput(255);
        pins = 0x39;
        Assert.Equal(irq, ppi.Read(2) & irq);
        Assert.Equal((byte?)0xa5, ppi.SetBus(false, false, true, port, 0));
        Assert.Equal(0, ppi.Read(2) & irq);
        Assert.Equal(full, ppi.Read(2) & full);
        ppi.SetBus(false, true, true, port, 0);
        Assert.Equal(0, ppi.Read(2) & (irq | full));
    }

    [Theory]
    [InlineData(0, 0xbb, 0x10, 0x08, 0x09)]
    [InlineData(1, 0x9f, 0x04, 0x01, 0x05)]
    public void InterruptEnableGatesAnUnreadInputAndLaterStrobesReplaceTheLatchedByte(
        int port, byte control, byte strobe, byte irq, byte enable)
    {
        byte pins = 0x12;
        var ppi = new Ppi8255 { ReadPortA = () => pins, ReadPortB = () => pins };
        ppi.Write(3, control);
        ppi.SetPortCInput((byte)~strobe); ppi.SetPortCInput(255);
        Assert.Equal(0, ppi.Read(2) & irq);
        ppi.Write(3, enable);
        Assert.Equal(irq, ppi.Read(2) & irq);
        ppi.Write(3, (byte)(enable & ~1));
        Assert.Equal(0, ppi.Read(2) & irq);
        pins = 0x69;
        ppi.SetPortCInput((byte)~strobe); ppi.SetPortCInput(255);
        ppi.Write(3, enable);
        Assert.Equal(0x69, ppi.Read(port));
        Assert.Equal(0, ppi.Read(2) & irq);
    }

    [Theory]
    [InlineData(0, 0xab, 0x40, 0x80, 0x08, 0x0d)]
    [InlineData(1, 0x9d, 0x04, 0x02, 0x01, 0x05)]
    public void StrobedOutputClearsInterruptAtWrStartAndBufferAtWrEndThenWaitsForAck(
        int port, byte control, byte ack, byte buffer, byte irq, byte enable)
    {
        var ppi = new Ppi8255();
        ppi.Write(3, control); ppi.Write(3, enable);
        Assert.Equal(irq | buffer, ppi.Read(2) & (irq | buffer));
        ppi.SetBus(false, true, false, port, 0x12);
        Assert.Equal(0, ppi.Read(2) & irq);
        Assert.Equal(buffer, ppi.Read(2) & buffer);
        Assert.Equal(0, ppi.Read(port));
        ppi.SetBus(false, true, false, port, 0xa5);
        ppi.SetBus(false, true, true, port, 0);
        Assert.Equal(0xa5, ppi.Read(port));
        Assert.Equal(0, ppi.Read(2) & (irq | buffer));
        ppi.SetPortCInput((byte)~ack);
        Assert.Equal(buffer, ppi.Read(2) & buffer);
        Assert.Equal(0, ppi.Read(2) & irq);
        ppi.SetPortCInput(255);
        Assert.Equal(irq | buffer, ppi.Read(2) & (irq | buffer));
        ppi.Read(port);
        Assert.Equal(irq, ppi.Read(2) & irq);
    }

    [Theory]
    [InlineData(0xc3)]
    [InlineData(0xe3)]
    public void BidirectionalAHasIndependentReadWriteInterruptsAndDrivesOnlyDuringAck(byte control)
    {
        var ppi = new Ppi8255 { ReadPortA = () => 0x69 };
        ppi.Write(3, control); ppi.Write(3, 0x09); ppi.Write(3, 0x0d);
        Assert.True(ppi.InterruptA);
        Assert.Equal(0, ppi.PortADriveMask);
        ppi.Write(0, 0xa5);
        Assert.False(ppi.InterruptA);
        ppi.SetPortCInput(0xbf);
        Assert.Equal(255, ppi.PortADriveMask);
        ppi.SetPortCInput(255);
        Assert.Equal(0, ppi.PortADriveMask);
        Assert.True(ppi.InterruptA);
        ppi.SetPortCInput(0xef); ppi.SetPortCInput(255);
        Assert.Equal(0x69, ppi.Read(0));
        Assert.True(ppi.InterruptA); // The independent acknowledged output remains ready.
        ppi.Write(0, 0x39);
        Assert.False(ppi.InterruptA);
    }

    [Fact]
    public void ModeOneAAndBAreIndependentAndLeavePc3UsableWhenAIsBasic()
    {
        var ppi = new Ppi8255 { ReadPortA = () => 0x39 };
        ppi.Write(3, 0xbd); // A input, B output, both strobed.
        ppi.Write(3, 0x09); ppi.Write(3, 0x05);
        Assert.True(ppi.InterruptB);
        ppi.SetPortCInput(0xef); ppi.SetPortCInput(255);
        Assert.True(ppi.InterruptA);
        ppi.Read(0);
        Assert.False(ppi.InterruptA);
        Assert.True(ppi.InterruptB);
        ppi.Write(3, 0x94); // B mode 1, A input mode 0; PC3 is a free output.
        ppi.Write(2, 0x08);
        Assert.Equal(0x08, ppi.Read(2) & 0x08);
        Assert.Equal(0x08, ppi.PortCDriveMask & 0x08);
    }

    [Fact]
    public void UnreservedUpperOutputBitsInStrobedARequireBsrAndByteWritesPreserveThem()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0xb0); // A input mode 1; PC6/7 free outputs.
        ppi.Write(2, 0xc7);
        Assert.Equal(0, ppi.Read(2) & 0xc0);
        Assert.Equal(7, ppi.Read(2) & 7);
        ppi.Write(3, 0x0d); ppi.Write(3, 0x0f);
        Assert.Equal(0xc0, ppi.Read(2) & 0xc0);
        ppi.Write(2, 0);
        Assert.Equal(0xc0, ppi.Read(2) & 0xc0);
    }
}
