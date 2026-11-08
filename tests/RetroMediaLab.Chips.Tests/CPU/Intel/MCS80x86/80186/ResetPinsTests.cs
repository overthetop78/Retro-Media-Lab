using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

namespace RetroMediaLab.Chips.Tests.CPU.Intel.MCS80x86.Chip80186;

public sealed class ResetPinsTests
{
    [Fact]
    public void HeldResetAdvancesClockWithoutTransactionsAndRetainsReleaseHalfCycle()
    {
        var reads = new List<long>();
        Cpu80186? cpu = null;
        cpu = new Cpu80186(_ => { reads.Add(cpu!.ClockHalfCount); return 0x90; },
            (_, _) => throw new InvalidOperationException(),
            _ => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException());
        var resetEdges = new List<(long Clock, bool High)>();
        cpu.ResetOutputChanged = high => resetEdges.Add((cpu.ClockHalfCount, high));
        cpu.SetReset(true);
        for (var clock = 0; clock < 8; clock++) cpu.Step();
        Assert.Equal(16, cpu.ClockHalfCount);
        Assert.Empty(reads);
        Assert.True(cpu.ResetOutputHigh);
        cpu.SetReset(false);
        for (var step = 0; step < 7; step++) cpu.Step();
        Assert.Equal(29, cpu.ClockHalfCount);
        Assert.Empty(reads);
        Assert.Equal(new[] { (0L, true), (20L, false) }, resetEdges);
        cpu.Step();
        Assert.NotEmpty(reads);
        Assert.Equal(29, reads[0]);
        Assert.All(reads, clock => Assert.True(clock >= 29));
        Assert.Equal(1, cpu.ClockHalfCount & 1);
    }

    [Fact]
    public void ReassertingResetCancelsRecoveryWithoutAccessingTheBus()
    {
        var reads = 0;
        var cpu = new Cpu80186(_ => { reads++; return 0x90; }, (_, _) => { }, _ => 0, (_, _) => { });
        cpu.SetReset(true);
        cpu.Step();
        cpu.SetReset(false);
        cpu.Step();
        cpu.SetReset(true);
        for (var clock = 0; clock < 4; clock++) cpu.Step();
        Assert.Equal(0, reads);
        Assert.True(cpu.ResetOutputHigh);
        Assert.Equal((ushort)0, cpu.IP);
        Assert.Equal((ushort)0xffff, cpu.Segment(1));
        cpu.SetReset(false);
        var released = cpu.ClockHalfCount;
        for (var step = 0; step < 7; step++) cpu.Step();
        Assert.Equal(released + 13, cpu.ClockHalfCount);
        Assert.Equal(0, reads);
        cpu.Step();
        Assert.True(reads > 0);
    }
}
