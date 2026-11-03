using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;

namespace RetroMediaLab.Chips.Tests.IO.Intel.MCS80.Chip8255;

public sealed partial class Ppi8255Tests
{
    [Theory]
    [InlineData("WASR")]
    [InlineData("WSAR")]
    [InlineData("WSRA")]
    [InlineData("SWAR")]
    [InlineData("SWRA")]
    [InlineData("SRWA")]
    public void AllDocumentedModeTwoOrderingsKeepTheTwoHandshakeDirectionsIndependent(string order)
    {
        var ppi = new Ppi8255 { ReadPortA = () => 0x69 };
        ppi.Write(3, 0xc0); ppi.Write(3, 0x09); ppi.Write(3, 0x0d);
        var pendingInput = false;
        var outputReady = true;
        foreach (var operation in order)
        {
            switch (operation)
            {
                case 'W': ppi.Write(0, 0xa5); outputReady = false; break;
                case 'A': ppi.SetPortCInput(0xbf); ppi.SetPortCInput(255); outputReady = true; break;
                case 'S': ppi.SetPortCInput(0xef); ppi.SetPortCInput(255); pendingInput = true; break;
                case 'R': Assert.Equal(0x69, ppi.Read(0)); pendingInput = false; break;
            }
            Assert.Equal(pendingInput || outputReady, ppi.InterruptA);
            Assert.Equal(pendingInput ? 0x20 : 0, ppi.Read(2) & 0x20);
            Assert.Equal(outputReady ? 0x80 : 0, ppi.Read(2) & 0x80);
            Assert.Equal(0, ppi.PortADriveMask);
        }
    }

    [Fact]
    public void ResetDuringAWriteCancelsItsTrailingTransfer()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0x80); ppi.Write(0, 0x12);
        var writes = new List<byte>();
        ppi.PortAPinsChanged = writes.Add;
        ppi.SetBus(false, true, false, 0, 0xa5);
        ppi.SetReset(true);
        writes.Clear();
        ppi.SetReset(false);
        ppi.SetBus(false, true, true, 0, 0);
        Assert.Empty(writes);
        Assert.Equal(0x9b, ppi.ControlWord);
        Assert.Equal(0, ppi.PortADriveMask);
    }

    [Fact]
    public void AResetAssertedWhileSamplingAnExternalInputReleasesTheDataBus()
    {
        var ppi = new Ppi8255();
        ppi.ReadPortA = () => { ppi.SetReset(true); return 0x69; };
        Assert.Null(ppi.SetBus(false, false, true, 0, 0));
        Assert.Equal(0, ppi.PortADriveMask | ppi.PortBDriveMask | ppi.PortCDriveMask);
    }

    [Fact]
    public void HandshakeInputPinsAreNotDrivenByTheirInterruptEnableBits()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0xc0);
        var driven = new List<(byte Data, byte Mask)>();
        ppi.PortCDriveChanged = (data, mask) => driven.Add((data, mask));
        ppi.Write(3, 0x09); ppi.Write(3, 0x0d);
        Assert.Equal(0, ppi.PortCDriveMask & 0x50);
        Assert.Equal(0x50, ppi.Read(2) & 0x50);
        Assert.All(driven, item => Assert.Equal(0, item.Mask & 0x50));
        ppi.SetPortCInput(0xaf);
        Assert.Equal(0x50, ppi.Read(2) & 0x50);
    }

    [Fact]
    public void EveryModeOneStatusBitSupportsTheDocumentedBitSetResetAccess()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0xbf); // Both groups input mode 1.
        foreach (var (set, clear, mask) in new[] { (0x0b, 0x0a, 0x20), (0x03, 0x02, 0x02) })
        {
            ppi.Write(3, (byte)set);
            Assert.Equal(mask, ppi.Read(2) & mask);
            ppi.Write(3, (byte)clear);
            Assert.Equal(0, ppi.Read(2) & mask);
        }
        ppi.Write(3, 0xa4); // Both groups output mode 1.
        foreach (var (set, clear, mask) in new[] { (0x0f, 0x0e, 0x80), (0x03, 0x02, 0x02) })
        {
            ppi.Write(3, (byte)clear);
            Assert.Equal(0, ppi.Read(2) & mask);
            ppi.Write(3, (byte)set);
            Assert.Equal(mask, ppi.Read(2) & mask);
        }
    }

    [Fact]
    public void BsrForcedInterruptsAreNotClearedByUnrelatedGpioOrTheOtherGroup()
    {
        var ppi = new Ppi8255();
        ppi.Write(3, 0xbf);
        ppi.Write(3, 0x07); // Force INTR A through its bit-set access.
        Assert.True(ppi.InterruptA);
        ppi.Write(3, 0x0f); // Free PC7 output latch; not an A handshake event.
        Assert.True(ppi.InterruptA);
        ppi.SetPortCInput(0x7f); // External free PC7 changes only.
        Assert.True(ppi.InterruptA);
        ppi.SetPortCInput(0x7b); // STB B falls; group A has no event.
        Assert.True(ppi.InterruptA);
        ppi.SetPortCInput(0x7f);
        Assert.True(ppi.InterruptA);
        ppi.Write(3, 0x01); // Force INTR B independently.
        Assert.True(ppi.InterruptB);
        ppi.Read(0); // A acknowledge must not clear forced INTR B.
        Assert.False(ppi.InterruptA);
        Assert.True(ppi.InterruptB);
        ppi.Read(1);
        Assert.False(ppi.InterruptB);
    }
}
