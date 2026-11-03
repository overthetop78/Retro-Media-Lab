using static RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551.Mos6551TestSignals;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551;

public sealed class Mos6551ReceiveTests
{
    [Fact]
    public void EveryShortFalseStartIsRejectedAndQualificationSamplesTheMiddle()
    {
        for (var length = 1; length < 8; length++)
        {
            var chip = Device();
            Rx(chip, false, length);
            Rx(chip, true, 200);
            Assert.Equal(0, chip.Read(1) & 8);
        }
        var receiver = Device();
        Rx(receiver, false, 1);
        Rx(receiver, true, 6);
        Rx(receiver, false, 1); // The midpoint, not every preceding sample, validates start.
        for (var bit = 0; bit < 8; bit++) Rx(receiver, (0xa5 & (1 << bit)) != 0);
        Rx(receiver, true, 17);
        Assert.Equal(0xa5, receiver.Read(0));
    }

    [Fact]
    public void OverrunPreservesUnreadDataAndDataReadClearsReceiveErrorsButNotIrq()
    {
        var chip = Device();
        Send(chip, 0x31);
        Send(chip, 0x32);
        Send(chip, 0x33);
        Assert.Equal(0x8c, chip.Read(1) & 0x8f);
        Assert.Equal(0x31, chip.Read(0));
        Assert.Equal(0, chip.Read(1) & 0x0f);
        Send(chip, 0x34);
        Assert.Equal(0x34, chip.Read(0));
        Assert.True(chip.Irq);
        Assert.Equal(0x80, chip.Read(1) & 0x8f);
    }

    [Fact]
    public void DtrCompletesAnActiveCharacterButPreventsTheNextStart()
    {
        var chip = Device();
        Rx(chip, false);
        for (var bit = 0; bit < 3; bit++) Rx(chip, (0xa5 & (1 << bit)) != 0);
        chip.Write(2, 0);
        for (var bit = 3; bit < 8; bit++) Rx(chip, (0xa5 & (1 << bit)) != 0);
        Rx(chip, true);
        Assert.Equal(8, chip.Read(1) & 0x88);
        Assert.Equal(0xa5, chip.Read(0));
        Send(chip, 0x53);
        Assert.Equal(0, chip.Read(1) & 8);
    }

    [Fact]
    public void DcdAbortsReceptionButDsrDoesNot()
    {
        foreach (var carrier in new[] { false, true })
        {
            var chip = Device();
            Rx(chip, false);
            Rx(chip, true);
            if (carrier) chip.SetDcd(true); else chip.SetDsr(true);
            for (var bit = 1; bit < 8; bit++) Rx(chip, false);
            Rx(chip, true);
            Assert.Equal(carrier ? 0 : 8, chip.Read(1) & 8);
        }
    }

    [Fact]
    public void ContinuousSpaceDeliversOneZeroWithFramingErrorThenWaitsForMark()
    {
        var chip = Device();
        Rx(chip, false, 16 * 12);
        Assert.Equal(0x0a, chip.Read(1) & 0x0f);
        Assert.Equal(0, chip.Read(0));
        Rx(chip, false, 16 * 30);
        Assert.Equal(0, chip.Read(1) & 8);
        Rx(chip, true);
        Send(chip, 0x55);
        Assert.Equal(8, chip.Read(1) & 0x0f);
        Assert.Equal(0x55, chip.Read(0));
    }
}
