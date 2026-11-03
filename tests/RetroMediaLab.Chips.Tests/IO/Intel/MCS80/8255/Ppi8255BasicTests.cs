using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Enums;
using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

namespace RetroMediaLab.Chips.Tests.IO.Intel.MCS80.Chip8255;

public sealed partial class Ppi8255Tests
{
    [Fact]
    public void EveryBasicDirectionCombinationSeparatesInputPinsFromOutputLatches()
    {
        var ppi = new Ppi8255 { ReadPortA = () => 0x55, ReadPortB = () => 0xaa, ReadPortC = () => 0x69 };
        for (var directions = 0; directions < 16; directions++)
        {
            var aInput = (directions & 1) != 0;
            var bInput = (directions & 2) != 0;
            var upperInput = (directions & 4) != 0;
            var lowerInput = (directions & 8) != 0;
            var control = (byte)(0x80 | (aInput ? 0x10 : 0) | (bInput ? 2 : 0)
                | (upperInput ? 8 : 0) | (lowerInput ? 1 : 0));
            ppi.Write(3, control);
            var inputsC = (upperInput ? 0xf0 : 0) | (lowerInput ? 0x0f : 0);
            for (var data = 0; data < 256; data++)
            {
                ppi.Write(0, (byte)data); ppi.Write(1, (byte)data); ppi.Write(2, (byte)data);
                Assert.Equal(aInput ? 0x55 : data, ppi.Read(0));
                Assert.Equal(bInput ? 0xaa : data, ppi.Read(1));
                Assert.Equal((data & ~inputsC) | (0x69 & inputsC), ppi.Read(2));
                Assert.Equal(aInput ? 0 : 255, ppi.PortADriveMask);
                Assert.Equal(bInput ? 0 : 255, ppi.PortBDriveMask);
                Assert.Equal((byte)~inputsC, ppi.PortCDriveMask);
            }
        }
    }

    [Fact]
    public void AllBsrCommandsSelectEveryBitAndIgnoreUnusedBitsWithoutChangingTheMode()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0x80);
        for (var bit = 0; bit < 8; bit++)
            for (var unused = 0; unused < 8; unused++)
            {
                ppi.Write(2, 0);
                ppi.Write(3, (byte)((unused << 4) | (bit << 1) | 1));
                Assert.Equal(1 << bit, ppi.Read(2));
                Assert.Equal(0x80, ppi.ControlWord);
                ppi.Write(3, (byte)((unused << 4) | (bit << 1)));
                Assert.Equal(0, ppi.Read(2));
            }
    }

    [Fact]
    public void EveryModeWordIsAcceptedAndClearsOutputAndInterruptState()
    {
        var ppi = new Ppi8255();
        for (var word = 0x80; word <= 255; word++)
        {
            ppi.Write(3, 0x80); ppi.Write(0, 255); ppi.Write(1, 255); ppi.Write(2, 255);
            ppi.Write(3, (byte)word);
            Assert.Equal(word, ppi.ControlWord);
            Assert.Equal((word & 0x40) != 0 ? PpiMode.Bidirectional
                : (word & 0x20) != 0 ? PpiMode.Strobed : PpiMode.Basic, ppi.GroupAMode);
            Assert.Equal((word & 4) != 0 ? PpiMode.Strobed : PpiMode.Basic, ppi.GroupBMode);
            Assert.False(ppi.InterruptA); Assert.False(ppi.InterruptB);
            if (ppi.GroupAMode != PpiMode.Bidirectional && (word & 0x10) == 0) Assert.Equal(0, ppi.Read(0));
            if ((word & 2) == 0) Assert.Equal(0, ppi.Read(1));
        }
    }

    [Fact]
    public void HeldResetReleasesAllPinsIgnoresWritesAndPreservesExternalLevels()
    {
        var ppi = new Ppi8255();
        ppi.SetPortCInput(0x55);
        ppi.Write(3, 0x80); ppi.Write(0, 0x12); ppi.Write(2, 0x34);
        ppi.SetReset(true);
        ppi.Write(3, 0x80); ppi.Write(0, 255);
        Assert.Equal(0, ppi.PortADriveMask | ppi.PortBDriveMask | ppi.PortCDriveMask);
        Assert.Equal(0x9b, ppi.ControlWord);
        Assert.Null(ppi.SetBus(false, false, true, 0, 0));
        Assert.False(ppi.InterruptA); Assert.False(ppi.InterruptB);
        ppi.SetReset(false);
        Assert.Equal(0x55, ppi.Read(2));
    }

    [Fact]
    public void ResetCallbackCanReadTheCompletedStateOfEveryPort()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0x80);
        var states = new List<(byte A, byte B, byte C)>();
        ppi.PortADriveChanged = (_, _) => states.Add((ppi.Read(0), ppi.Read(1), ppi.Read(2)));
        ppi.Reset();
        Assert.Equal(((byte)255, (byte)255, (byte)255), states.Single());
    }

    [Fact]
    public void ReadingControlDoesNotExposeARegisterAndBasicInputsRemainLiveDuringRd()
    {
        byte input = 0x12;
        var ppi = new Ppi8255 { ReadPortA = () => input };
        Assert.Null(ppi.SetBus(false, false, true, 3, 0));
        ppi.SetBus(false, true, true, 0, 0);
        Assert.Equal((byte?)0x12, ppi.SetBus(false, false, true, 0, 0));
        input = 0xa5;
        Assert.Equal((byte?)0xa5, ppi.SetBus(false, false, true, 0, 0));
        ppi.SetBus(false, true, true, 0, 0);
    }

    [Fact]
    public void WriteDataIsCapturedAtWrEndAndHeldLowDoesNotRepeatTheTransfer()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0x80);
        var writes = new List<byte>();
        ppi.PortAPinsChanged = writes.Add;
        Assert.Null(ppi.SetBus(false, true, false, 0, 0x12));
        Assert.Equal(0, ppi.Read(0));
        Assert.Null(ppi.SetBus(false, true, false, 0, 0xa5));
        Assert.Empty(writes);
        ppi.SetBus(false, true, true, 0, 0);
        Assert.Equal(0xa5, ppi.Read(0));
        Assert.Equal(new byte[] { 0xa5 }, writes);
    }

    [Fact]
    public void DeselectedAndIllegalReadWriteCombinationsDoNotTransferData()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0x80); ppi.Write(0, 0x39);
        Assert.Null(ppi.SetBus(true, true, false, 0, 255));
        Assert.Null(ppi.SetBus(true, false, true, 0, 0));
        Assert.Null(ppi.SetBus(false, false, false, 0, 255));
        ppi.SetBus(false, true, true, 0, 0);
        Assert.Equal(0x39, ppi.Read(0));
    }
}
