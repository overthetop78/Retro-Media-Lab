using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;
using static RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551.Mos6551TestSignals;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551;

public sealed class Mos6551ClockTests
{
    public static IEnumerable<object[]> Divisors() => new[] { 1, 2304, 1536, 1048, 856, 768, 384, 192, 96, 64, 48, 32, 24, 16, 12, 6 }
        .Select((divider, selection) => new object[] { selection, divider });

    [Theory]
    [MemberData(nameof(Divisors))]
    public void CrystalAndExplicitXtalProduceTheSameDividedFallingEdges(int selection, int divider)
    {
        var external = Device((byte)selection);
        var crystal = new Mos6551(1_843_200);
        crystal.SetCts(false);
        crystal.Write(2, 9);
        crystal.Write(3, (byte)selection);
        external.Write(0, 0xa5);
        crystal.Write(0, 0xa5);
        Tx(external, divider - 1);
        crystal.Advance(divider - 1);
        Assert.True(external.Txd);
        Assert.True(crystal.Txd);
        Tx(external);
        crystal.Advance(1);
        Assert.False(external.Txd);
        Assert.False(crystal.Txd);
        for (var clock = 0; clock < 16 * 11; clock++)
        {
            Tx(external, divider);
            crystal.Advance(divider);
            Assert.Equal(external.Txd, crystal.Txd);
        }
    }

    [Fact]
    public void RisingReceiveEdgesAreIndependentOfTransmitEdgesAndIgnoreRepeatedLevels()
    {
        var receiver = Device(1);
        receiver.Write(0, 0xa5);
        receiver.SetReceiveClock(false);
        receiver.SetReceiveClock(false);
        Send(receiver, 0x93);
        Assert.Equal(0x93, receiver.Read(0));
        Assert.True(receiver.Txd);
        Assert.Equal(0, receiver.Read(1) & 0x10);
    }

    [Fact]
    public void InternalClockIsVisibleOnRxcAndExternalRxcIsThenIgnored()
    {
        var chip = Device(0x10);
        var levels = new List<bool>();
        chip.ReceiveClockChanged = levels.Add;
        try
        {
            Assert.True(chip.ReceiveClockIsOutput);
            chip.SetRxd(false);
            chip.AdvanceReceiveClock(1000);
            Assert.Equal(0, chip.Read(1) & 8);
            Tx(chip, 2);
            Assert.Equal(new[] { false, true, false }, levels);
            Assert.False(chip.ReceiveClockOutput);
        }
        finally { chip.ReceiveClockChanged = null; }
    }

    [Fact]
    public void DisabledExternalClockBatchesRetainPhaseWithoutOverflowOrWorkPerEdge()
    {
        var chip = new Mos6551(1, 0);
        chip.AdvanceReceiveClock(int.MaxValue);
        chip.SetDcd(false);
        chip.Write(2, 9);
        chip.SetRxd(false);
        chip.AdvanceReceiveClock(1); // Falling edge after the odd disabled batch.
        chip.AdvanceReceiveClock(14); // Seven rising edges, not yet midpoint.
        Assert.Equal(0, chip.Read(1) & 8);
        chip.AdvanceReceiveClock(2);
        for (var bit = 0; bit < 8; bit++) Rx(chip, (0xa5 & (1 << bit)) != 0);
        Rx(chip, true, 17);
        Assert.Equal(0xa5, chip.Read(0));
    }

    [Fact]
    public void BatchedAdvancePreservesFractionalClocksAndOutputEdges()
    {
        var single = new Mos6551(2_000_003);
        var batch = new Mos6551(2_000_003);
        var a = new List<bool>();
        var b = new List<bool>();
        single.TransmitChanged = a.Add;
        batch.TransmitChanged = b.Add;
        try
        {
            foreach (var chip in new[] { single, batch })
            {
                chip.SetCts(false); chip.Write(2, 5); chip.Write(3, 0x1f); chip.Write(0, 0xa5);
            }
            for (var tick = 0; tick < 10349; tick++) single.Advance(1);
            batch.Advance(10349);
            Assert.Equal(a, b);
            Assert.Equal(single.Read(1), batch.Read(1));
        }
        finally { single.TransmitChanged = batch.TransmitChanged = null; }
    }
}
