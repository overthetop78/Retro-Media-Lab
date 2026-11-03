using static RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551.Mos6551TestSignals;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551;

public sealed class Mos6551TransmitTests
{
    [Fact]
    public void EmptyTransmitterInterruptsPeriodicallyEvenWithCtsHighAndNewDataStartsWithoutWaiting()
    {
        var chip = Device(0, 5);
        chip.SetCts(true);
        Tx(chip, 2);
        Assert.Equal(0x80, chip.Read(1) & 0x90);
        Tx(chip, 160);
        Assert.Equal(0x80, chip.Read(1) & 0x90);
        chip.Write(0, 0xa5);
        chip.SetCts(false);
        Tx(chip);
        Assert.False(chip.Txd);
        Assert.Equal(0x10, chip.Read(1) & 0x10); // TDRE precedes the IRQ latch.
        Tx(chip);
        Assert.True(chip.Irq);
    }

    [Fact]
    public void CtsAbortsCurrentCharacterWithoutResumingItsTailAndPreservesTheQueuedByte()
    {
        var chip = Device();
        chip.Write(0, 0x00);
        Tx(chip, 25);
        chip.SetCts(true);
        Assert.True(chip.Txd);
        Tx(chip, 20);
        chip.SetCts(false);
        Tx(chip, 30);
        Assert.True(chip.Txd);
        chip.Write(0, 0xa5);
        Tx(chip);
        Assert.False(chip.Txd);
        chip.Write(0, 0x53);
        chip.SetCts(true);
        Tx(chip, 13);
        chip.SetCts(false);
        Tx(chip);
        Assert.False(chip.Txd);
        Tx(chip, 16);
        Assert.True(chip.Txd); // Bit 0 of queued 53.
        Assert.Equal(0x10, chip.Read(1) & 0x10);
    }

    [Fact]
    public void RtsShutdownCompletesTheActiveFrame()
    {
        var chip = Device();
        chip.Write(0, 0xa5);
        Tx(chip);
        chip.Write(2, 1);
        var frame = Frame(8, 0, 0xa5);
        foreach (var bit in frame.Skip(1))
        {
            Tx(chip, 16);
            Assert.Equal(bit, chip.Txd);
        }
        chip.Write(0, 0x00);
        Tx(chip, 32);
        Assert.True(chip.Txd);
        Assert.Equal(0, chip.Read(1) & 0x10);
    }

    [Fact]
    public void EvenBriefBreakSelectionProducesAFullBreakThenAStopBeforeData()
    {
        var chip = Device();
        chip.Write(2, 0x0d);
        chip.Write(2, 9);
        Tx(chip);
        Assert.False(chip.Txd);
        Tx(chip, 159);
        Assert.False(chip.Txd);
        Tx(chip);
        Assert.True(chip.Txd);
        chip.Write(0, 0);
        Tx(chip, 15);
        Assert.True(chip.Txd);
        Tx(chip);
        Assert.False(chip.Txd);
    }

    [Fact]
    public void EchoOverrunStopsOutputUntilDataIsReadAndAnotherStartIsRecognized()
    {
        var chip = Device(0, 0x11);
        void Both(bool value, int count = 16)
        {
            for (var clock = 0; clock < count; clock++) { Rx(chip, value, 1); Tx(chip); }
        }
        void Character(byte value)
        {
            Both(false);
            for (var bit = 0; bit < 8; bit++) Both((value & (1 << bit)) != 0);
            Both(true);
        }
        Character(0x53);
        Character(0x55);
        Assert.Equal(0x0c, chip.Read(1) & 0x0f);
        Assert.True(chip.Txd);
        Assert.Equal(0x53, chip.Read(0));
        Both(false, 2);
        Both(true, 2);
        Assert.True(chip.Txd);
        Both(true, 20);
        Both(false, 12);
        Assert.False(chip.Txd);
        chip.SetCts(true);
        Assert.True(chip.Txd);
        Both(false, 8);
        Assert.True(chip.Txd);
    }

    [Fact]
    public void EchoWaitsHalfABitAndCanBeDisabledImmediately()
    {
        var chip = Device(0x10, 0x11);
        chip.SetRxd(false);
        Tx(chip, 8);
        Assert.True(chip.Txd);
        Tx(chip);
        Assert.False(chip.Txd);
        chip.Write(2, 1);
        Assert.True(chip.Txd);
        Assert.True(chip.Rts);
    }
}
