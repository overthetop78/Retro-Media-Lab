using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class CpuClockTests
{
    private sealed class Tape : IElectronTapePins
    {
        public bool InputPresent => false;
        public bool InputLevel => false;
        public void SetOutputLevel(bool value) { }
        public void Tick(bool motor) { }
    }

    private sealed class Probe
    {
        internal readonly ElectronVideo Video;
        internal readonly ElectronUla Ula;
        internal readonly List<(bool High, int Point)> Edges = [];
        internal int Cycles;

        internal Probe()
        {
            Video = new(_ => 0);
            Ula = new(Video, new Tape());
            Ula.CpuClockChanged = (high, point) => Edges.Add((high, Cycles * 8 + (point & 7)));
        }

        internal void Tick()
        {
            Ula.Tick();
            Cycles++;
        }

        internal int Finish()
        {
            var ticks = 0;
            while (Ula.CpuWaiting && ++ticks <= 128) Tick();
            Assert.False(Ula.CpuWaiting);
            Ula.EndCpuCycle();
            return ticks;
        }
    }

    [Fact]
    public void RomClockHasFourLowAndFourHighMasterPeriodsWithoutChangingCpuCadence()
    {
        var probe = new Probe();
        for (var cycle = 0; cycle < 160; cycle++)
        {
            probe.Ula.BeginCpuCycle((ushort)(0xc000 + cycle), false, 0);
            probe.Tick();
            probe.Ula.EndCpuCycle();
            Assert.False(probe.Ula.CpuWaiting);
            Assert.True(probe.Ula.CpuClockHigh);
        }
        Assert.Equal(320, probe.Edges.Count);
        for (var cycle = 0; cycle < 160; cycle++)
        {
            Assert.Equal((false, cycle * 8), probe.Edges[cycle * 2]);
            Assert.Equal((true, cycle * 8 + 4), probe.Edges[cycle * 2 + 1]);
        }
    }

    [Theory]
    [InlineData(0, 0x0200, 12)]
    [InlineData(1, 0x0200, 20)]
    [InlineData(0, 0xfc00, 12)]
    [InlineData(1, 0xfc00, 20)]
    [InlineData(0, 0x8000, 12)]
    [InlineData(1, 0x8000, 20)]
    public void BothOneMhzTransitionsStretchHighAndNeverRepeatTheLowPart(int prefix, int address, int highPoints)
    {
        var probe = new Probe();
        probe.Ula.Write(7, 0x20);
        probe.Ula.Write(5, 8);
        for (var cycle = 0; cycle < prefix; cycle++) probe.Tick();
        probe.Ula.BeginCpuCycle((ushort)address, false, 0);
        probe.Tick();
        var previousRise = probe.Edges[^1].Point;
        var count = probe.Edges.Count;
        Assert.Equal(prefix == 0 ? 1 : 2, probe.Finish());
        Assert.Equal(count, probe.Edges.Count);
        Assert.True(probe.Ula.CpuClockHigh);
        var nextFall = probe.Cycles * 8;
        probe.Tick();
        Assert.Equal((false, nextFall), probe.Edges[count]);
        Assert.Equal(highPoints, nextFall - previousRise);
        Assert.Equal((true, nextFall + 4), probe.Edges[count + 1]);
    }

    [Fact]
    public void ActiveVideoHoldsPhiHighUntilThePendingRamTransactionFinishes()
    {
        var probe = new Probe();
        probe.Ula.BeginCpuCycle(0x0200, false, 0);
        Assert.Equal(82, probe.Finish());
        Assert.Equal(new[] { (false, 0), (true, 4) }, probe.Edges);
        Assert.Equal(656, probe.Video.Dot);
        probe.Tick();
        Assert.Equal((false, 656), probe.Edges[2]);
        Assert.Equal((true, 660), probe.Edges[3]);
    }

    [Fact]
    public void NmiReleasesTheRamWaitAtTheOneMhzBoundaryWithoutAnExtraPhiPulse()
    {
        var probe = new Probe();
        probe.Ula.BeginCpuCycle(0x0200, false, 0);
        for (var cycle = 0; cycle < 8; cycle++) probe.Tick();
        Assert.True(probe.Ula.CpuWaiting);
        probe.Ula.Nmi = true;
        Assert.Equal(2, probe.Finish());
        Assert.Equal(new[] { (false, 0), (true, 4) }, probe.Edges);
        var resumed = probe.Cycles * 8;
        probe.Tick();
        Assert.Equal((false, resumed), probe.Edges[2]);
        Assert.True(probe.Video.DisplayFetching);
    }

    [Fact]
    public void BreakPreservesTheClockAndOutstandingAccessWhilePowerOnCancelsTheWait()
    {
        var actual = new Probe();
        var expected = new Probe();
        foreach (var probe in new[] { actual, expected })
        {
            probe.Ula.BeginCpuCycle(0x0200, false, 0);
            for (var cycle = 0; cycle < 4; cycle++) probe.Tick();
        }
        actual.Ula.Reset(false);
        Assert.Equal(expected.Finish(), actual.Finish());
        Assert.Equal(expected.Edges, actual.Edges);
        actual.Tick();
        expected.Tick();
        Assert.Equal(expected.Edges, actual.Edges);
        actual.Ula.BeginCpuCycle(0x0200, false, 0);
        actual.Ula.Reset(true);
        Assert.False(actual.Ula.CpuWaiting);
        var point = actual.Cycles * 8;
        actual.Tick();
        Assert.Contains((false, point), actual.Edges);
        Assert.Contains((true, point + 4), actual.Edges);
    }

    [Fact]
    public void AlignedCpuRamSamplesStraddleThePhiRisingEdge()
    {
        ElectronUla? ula = null;
        List<bool> samples = [];
        var video = new ElectronVideo(address =>
        {
            if (address == 0x0200)
            {
                samples.Add(ula!.CpuClockHigh);
            }
            return 0;
        });
        ula = new(video, new Tape());
        ula.Write(7, 0x20);
        ula.BeginCpuCycle(0x0200, false, 0);
        while (ula.CpuWaiting) ula.Tick();
        Assert.Equal(new[] { false, true }, samples);
        ula.EndCpuCycle();
    }
    [Fact]
    public void HoldInputChangesBetweenScheduledEdgesAreAppliedOnTheNextPoint()
    {
        var video = new ElectronVideo(_ => 0);
        List<(bool High, int Point)> edges = [];
        video.CpuClockChanged = (high, point) => edges.Add((high, point));
        video.TickPixel();
        video.ConfigureCpuRam(false, false, true);
        video.TickPixel();
        video.ConfigureCpuRam(false, false, false);
        video.TickPixel();
        video.TickPixel();
        video.TickPixel();
        Assert.Equal(new[] { (false, 0), (true, 1), (false, 2), (true, 4) }, edges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReentrantHoldChangesFromTheEdgeCallbackAreNotDiscarded(bool aligned)
    {
        var video = new ElectronVideo(_ => 0);
        List<(bool High, int Point)> edges = [];
        video.CpuClockChanged = (high, point) =>
        {
            edges.Add((high, point));
            if (point == 0) video.ConfigureCpuRam(false, false, true);
            if (point == 1) video.ConfigureCpuRam(false, false, false);
        };
        if (aligned) video.Tick();
        else for (var point = 0; point < 5; point++) video.TickPixel();
        Assert.Equal(new[] { (false, 0), (true, 1), (false, 2), (true, 4) }, edges);
    }

}
